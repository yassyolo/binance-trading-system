using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016TradeExecutor(
    IOptions<Bot8016Options> options,
    Bot8016OrderExecutionService execution,
    Bot8016SignalContextStore signalContexts,
    ITradingSignalContextAccessor signalContext,
    IPositionStore positions,
    BinanceProtectedPositionService commonOrders,
    IClock clock,
    ILogger<Bot8016TradeExecutor> logger) : IBotTradeExecutor
{
    private readonly Bot8016Options _options = options.Value;
    public string BotName => _options.BotName;

    public async Task<TradeExecutionResult> OpenAsync(string symbol, PositionSide side, string? source, CancellationToken ct)
    {
        if (!symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase))
            return TradeExecutionResult.Failure($"{BotName} does not support {symbol}.");
        var current = signalContext.Current;
        if (current is null || !signalContexts.TryGet(current.SignalId, out var entrySignal))
            return TradeExecutionResult.Failure("BOT8016 entry context was not available for live execution.");
        if (entrySignal.Side != side)
            return TradeExecutionResult.Failure("BOT8016 entry context side does not match the approved trading signal.");
        try
        {
            var position = await execution.OpenAsync(entrySignal, ct);
            position.Source = string.IsNullOrWhiteSpace(source) ? "alligator" : source.Trim();
            await positions.SaveAsync(position, ct);
            return TradeExecutionResult.Success(position.ShortId, $"BOT8016 opened {side} position.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "BOT8016 live open failed. Side = {Side}, Symbol = {Symbol}", side, symbol);
            return TradeExecutionResult.Failure(exception.Message, exception);
        }
    }

    public async Task<TradeExecutionResult> CloseAsync(string shortId, string reason, CancellationToken ct)
    {
        var position = await positions.GetAsync(_options.BotName, shortId, ct);
        if (position is null) return TradeExecutionResult.Failure($"Position {shortId} not found.");
        if (position.Closed) return TradeExecutionResult.Success(shortId, "Already closed.");
        try
        {
            await commonOrders.CloseAsync(position, ct);
            position.MarkClosed(reason, clock.UtcNow);
            await positions.SaveAsync(position, ct);
            return TradeExecutionResult.Success(shortId, reason);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "BOT8016 live close failed. ShortId = {ShortId}", shortId);
            return TradeExecutionResult.Failure(exception.Message, exception);
        }
    }
}
