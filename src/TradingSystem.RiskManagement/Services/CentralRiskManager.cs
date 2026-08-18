using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Risk.Contracts;
using TradingSystem.Application.Risk.Models;
using TradingSystem.Application.Time;
using TradingSystem.Domain.Enums;
using TradingSystem.PortfolioManagement.Provider;
using TradingSystem.RiskManagement.Configuration;
using TradingSystem.RiskManagement.Contracts;
using TradingSystem.RiskManagement.Models;

namespace TradingSystem.RiskManagement.Services;

public sealed class CentralRiskManager(
    IOptions<CentralRiskOptions> options,
    IPortfolioSnapshotProvider portfolio,
    IRiskOrderSizingProvider orderSizing,
    IRiskStateProvider riskStateProvider,
    IRiskAdmissionReservationStore reservations,
    IClock clock,
    ILogger<CentralRiskManager> logger)
    : ICentralRiskManager,
      IRiskAdmissionLifecycle
{
    private readonly CentralRiskOptions _options = options.Value;
    private readonly SemaphoreSlim _admissionGate = new(1, 1);

    public async Task<RiskDecision> EvaluateOpenAsync(RiskEvaluationContext context, CancellationToken ct)
    {
        if (!_options.Enabled)
            return RiskDecision.Allow("Central risk management is disabled.");

        await _admissionGate.WaitAsync(ct);
        try
        {
            var now = clock.UtcNow;

            portfolio.Invalidate();
            var snapshot = await portfolio.GetSnapshotAsync(ct);

            var sizing = await orderSizing.GetAsync(context.Signal.BotName, ct);
            if (sizing is null || sizing.Quantity <= 0)
                return RiskDecision.Block("ORDER_SIZE_UNKNOWN", $"No risk order size is configured for bot '{context.Signal.BotName}'.");

            var candidateNotional = context.MarkPrice * sizing.Quantity;
            if (candidateNotional <= 0)
                return RiskDecision.Block("INVALID_NOTIONAL", "Candidate order notional is invalid.");

            if (sizing.MaximumNotional is > 0 &&
                candidateNotional > sizing.MaximumNotional.Value)
                return RiskDecision.Block("BOT_NOTIONAL_LIMIT", $"Candidate notional {candidateNotional:F2} exceeds bot limit {sizing.MaximumNotional.Value:F2}.");

            var riskState = await riskStateProvider.GetAsync(context.EvaluatedAtUtc, ct);

            if (_options.BlockWhenReconciliationHasCriticalFindings &&
                riskState.HasCriticalReconciliationFindings)
                return RiskDecision.Block("CRITICAL_RECONCILIATION", "Critical unresolved reconciliation findings exist.");

            if (_options.MaximumDailyLoss > 0 &&
                riskState.DailyRealizedPnl <= -_options.MaximumDailyLoss)
                return RiskDecision.Block("DAILY_LOSS_LIMIT", $"Daily realized PnL {riskState.DailyRealizedPnl:F2} reached the loss limit {-_options.MaximumDailyLoss:F2}.");

            var dailyDrawdownPercent = riskState.DailyPeakEquity <= 0
                ? 0m
                : Math.Max(riskState.DailyPeakEquity - riskState.CurrentEquity, 0m) / riskState.DailyPeakEquity * 100m;

            if (_options.MaximumDailyDrawdownPercent > 0 &&
                dailyDrawdownPercent >= _options.MaximumDailyDrawdownPercent)
                return RiskDecision.Block("DAILY_DRAWDOWN_LIMIT", $"Daily drawdown {dailyDrawdownPercent:F2}% reached the limit {_options.MaximumDailyDrawdownPercent:F2}%.");

            if (_options.MaximumConsecutiveLosses > 0 &&
                riskState.ConsecutiveLosses >= _options.MaximumConsecutiveLosses)
                return RiskDecision.Block("CONSECUTIVE_LOSSES_LIMIT", $"Consecutive losses {riskState.ConsecutiveLosses} reached the limit {_options.MaximumConsecutiveLosses}.");

            var activeReservations = reservations.GetActive(now);
            var reservedCount = activeReservations.Count;
            var reservedForBot = activeReservations.Count(x => x.BotName.Equals(context.Signal.BotName, StringComparison.OrdinalIgnoreCase));
            var reservedForSymbol = activeReservations.Count(x => x.Symbol.Equals(context.Signal.Symbol, StringComparison.OrdinalIgnoreCase));
            var reservedNotional = activeReservations.Sum(x => x.Notional);

            if (snapshot.OpenPositions + reservedCount >= _options.MaximumOpenPositions)
                return RiskDecision.Block("MAX_OPEN_POSITIONS", $"Maximum total open positions reached. Current = {snapshot.OpenPositions}, Reserved = {reservedCount}, Limit = {_options.MaximumOpenPositions}.");

            var bot = snapshot.Bots.FirstOrDefault(x => x.BotName.Equals(context.Signal.BotName, StringComparison.OrdinalIgnoreCase));

            var botPositions = bot?.OpenPositions ?? 0;

            if (botPositions + reservedForBot >= _options.MaximumOpenPositionsPerBot)
                return RiskDecision.Block("MAX_OPEN_POSITIONS_PER_BOT", $"Maximum positions for '{context.Signal.BotName}' reached. Current = {botPositions}, Reserved = {reservedForBot}, Limit = {_options.MaximumOpenPositionsPerBot}.");

            var symbol = snapshot.Symbols.FirstOrDefault(x => x.Symbol.Equals(context.Signal.Symbol, StringComparison.OrdinalIgnoreCase));

            var symbolPositions = symbol?.OpenPositions ?? 0;

            if (symbolPositions + reservedForSymbol >= _options.MaximumOpenPositionsPerSymbol)
                return RiskDecision.Block("MAX_OPEN_POSITIONS_PER_SYMBOL", $"Maximum positions for '{context.Signal.Symbol}' reached. Current = {symbolPositions}, Reserved = {reservedForSymbol}, Limit = {_options.MaximumOpenPositionsPerSymbol}.");

            var projectedTotalNotional = snapshot.GrossNotional + reservedNotional + candidateNotional;

            if (projectedTotalNotional > _options.MaximumEstimatedNotional)
                return RiskDecision.Block("MAX_ESTIMATED_NOTIONAL", $"Projected gross notional {projectedTotalNotional:F2} exceeds {_options.MaximumEstimatedNotional:F2}.");

            var symbolReservations = activeReservations.Where(x => x.Symbol.Equals(context.Signal.Symbol, StringComparison.OrdinalIgnoreCase)).ToArray();

            var currentSymbolGross = (symbol?.GrossNotional ?? 0m) + symbolReservations.Sum(x => x.Notional);

            var projectedSymbolGross = currentSymbolGross + candidateNotional;

            if (_options.MaximumGrossNotionalPerSymbol > 0 &&
                projectedSymbolGross > _options.MaximumGrossNotionalPerSymbol)
                return RiskDecision.Block("MAX_SYMBOL_GROSS_NOTIONAL", $"Projected gross notional for '{context.Signal.Symbol}' is {projectedSymbolGross:F2}.");

            var candidateSignedNotional = context.Signal.Side == PositionSide.Long ? candidateNotional : -candidateNotional;

            var reservedSigned = symbolReservations.Sum(x => x.Side == PositionSide.Long ? x.Notional : -x.Notional);

            var projectedSymbolNet = (symbol?.NetNotional ?? 0m) + reservedSigned + candidateSignedNotional;

            if (_options.MaximumAbsoluteNetNotionalPerSymbol > 0 &&
                Math.Abs(projectedSymbolNet) > _options.MaximumAbsoluteNetNotionalPerSymbol)
                return RiskDecision.Block("MAX_SYMBOL_NET_NOTIONAL", $"Projected net notional for '{context.Signal.Symbol}' is {projectedSymbolNet:F2}.");

            reservations.Add(new RiskAdmissionReservation(
                Guid.NewGuid(),
                context.Signal.SignalId,
                context.Signal.BotName,
                context.Signal.Symbol,
                context.Signal.Side,
                sizing.Quantity,
                candidateNotional,
                now.AddSeconds(_options.AdmissionReservationSeconds)));

            logger.LogInformation("Risk admission allowed. SignalId = {SignalId}, Bot = {Bot}, Symbol = {Symbol}, Side = {Side}, CandidateNotional = {Notional}, OpenPositions = {OpenPositions}, ProjectedGross = {ProjectedGross}",
                context.Signal.SignalId,
                context.Signal.BotName,
                context.Signal.Symbol,
                context.Signal.Side,
                candidateNotional,
                snapshot.OpenPositions,
                projectedTotalNotional);

            return RiskDecision.Allow(
                $"Risk checks passed. Candidate notional = {candidateNotional:F2}, projected gross = {projectedTotalNotional:F2}.");
        }
        finally
        {
            _admissionGate.Release();
        }
    }

    public async Task CompleteAsync(string signalId, bool executionSucceeded, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(signalId))
            return;

        await _admissionGate.WaitAsync(ct);
        try
        {
            if (executionSucceeded)
                portfolio.Invalidate();

            if (reservations.RemoveBySignalId(signalId))
                logger.LogInformation("Risk admission reservation completed. SignalId = {SignalId}, ExecutionSucceeded = {ExecutionSucceeded}", signalId, executionSucceeded);
        }
        finally
        {
            _admissionGate.Release();
        }
    }
}
