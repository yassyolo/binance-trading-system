using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.EventStore.Constants;
using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.Models;
using TradingSystem.Observability.History.Models;
using TradingSystem.Observability.Pipeline;
using TradingSystem.PaperTrading.Configuration;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.PaperTrading.Models;
using TradingSystem.PaperTrading.Models.Enums;

namespace TradingSystem.PaperTrading.Executor;

public sealed class PaperTradeExecutor(
    IPaperTradingStore store,
    IMarketPriceProvider markPriceProvider,
    IBotRuntimeConfigurationProvider configProvider,
    ITradingPipelineRecorder tradingPipelineRecorder,
    ITradingEventStore eventStore,
    ITradingSignalContextAccessor signalContext,
    IOptions<PaperTradingOptions> options,
    ILogger<PaperTradeExecutor> logger)
{
    private const string PaperEnvironment = "Paper";
    private const string UnknownStrategyVersion = "unknown";

    private readonly PaperTradingOptions _options = options.Value;

    public async Task<TradeExecutionResult> OpenAsync(string botName, string symbol, PositionSide side, string? source, CancellationToken ct)
    {
        if (!_options.Enabled)
            return TradeExecutionResult.Failure("Paper trading is disabled.");

        var openPositions = await store.GetOpenAsync(ct);
        if (openPositions.Count >= _options.MaximumOpenPositions)
            return TradeExecutionResult.Failure("Paper trading maximum open positions limit reached.");

        var config = await configProvider.GetAsync(botName, ct);
        if (config is null)
            return TradeExecutionResult.Failure($"Runtime config for {botName} was not found.");

        if (config.Quantity <= 0)
            return TradeExecutionResult.Failure($"Invalid paper trading quantity configured for {botName}.");

        var normalizedSymbol = symbol.ToUpperInvariant();
        var markPrice = await markPriceProvider.GetMarkPriceAsync(normalizedSymbol, ct);
        if (markPrice <= 0)
            return TradeExecutionResult.Failure($"Invalid mark price for {normalizedSymbol}.");

        var entryPrice = ApplySlippage(markPrice, side, opening: true);
        var quantity = config.Quantity;
        var takeProfitPrice = ResolveTakeProfitPrice(entryPrice, side, config.ProfitDistance);
        var stopLossPrice = ApplyPercent(entryPrice, side, _options.DefaultStopLossPercent, favorable: false);
        var entryFee = CalculateFee(entryPrice, quantity);
        var openedAtUtc = DateTime.UtcNow;

        var executionContext = signalContext.Current;

        var position = new PaperTradingPosition
        {
            PositionId = Guid.NewGuid(),
            ShortId = CreateShortId(openedAtUtc),
            SignalId = executionContext?.SignalId,
            StrategyVersion = executionContext?.StrategyVersion ?? UnknownStrategyVersion,
            BotName = botName,
            Symbol = normalizedSymbol,
            Side = side,
            Quantity = quantity,
            EntryPrice = entryPrice,
            TakeProfitPrice = takeProfitPrice,
            StopLossPrice = stopLossPrice,
            EntryFee = entryFee,
            ExitPrice = null,
            ExitFee = null,
            RealizedPnl = null,
            Status = PaperPositionStatus.Open,
            Source = NormalizeSource(source),
            OpenedAtUtc = openedAtUtc,
            ClosedAtUtc = null,
            CloseReason = null,
            Version = 1
        };

        await store.CreateAsync(position, ct);
        
        await TryRecordPositionOpenedAsync(position, ct);
        
        await TryRecordPositionEventAsync(
            position,
            TradingEventTypes.PositionOpened,
            new
            {
                position.ShortId,
                position.Symbol,
                Side = position.Side.ToString(),
                position.Quantity,
                position.EntryPrice,
                position.TakeProfitPrice,
                position.StopLossPrice,
                position.EntryFee,
                position.Source
            },
            position.OpenedAtUtc,
            ct);

        return TradeExecutionResult.Success(position.ShortId, $"Paper p opened at {entryPrice}.");
    }

    public async Task<TradeExecutionResult> CloseAsync(string botName, string shortId, string reason, CancellationToken ct)
    {
        var position = await store.GetAsync(botName, shortId, ct);
        
        var validationResult = ValidatePositionForClose(position, shortId);
        if (validationResult is not null)
            return validationResult;

        var markPrice = await markPriceProvider.GetMarkPriceAsync(position!.Symbol, ct);
        if (markPrice <= 0)
            return TradeExecutionResult.Failure($"Invalid mark price for {position.Symbol}.");

        return await ClosePositionAtPriceAsync(position, markPrice, reason, ct);
    }

    public async Task<TradeExecutionResult> CloseAtPriceAsync(string botName, string shortId, decimal triggerPrice, string reason, CancellationToken ct)
    {
        if (triggerPrice <= 0)
            return TradeExecutionResult.Failure("Paper p trigger price must be positive.");

        var position = await store.GetAsync(botName, shortId, ct);
       
        var validationResult = ValidatePositionForClose(position, shortId);
        if (validationResult is not null)
            return validationResult;

        return await ClosePositionAtPriceAsync(position!, triggerPrice, reason, ct);
    }

    private async Task<TradeExecutionResult> ClosePositionAtPriceAsync(PaperTradingPosition p, decimal marketPrice, string reason, CancellationToken ct)
    {
        var exitPrice = ApplySlippage(marketPrice, p.Side, opening: false);
        var exitFee = CalculateFee(exitPrice, p.Quantity);
        var grossPnl = CalculateGrossPnl(p, exitPrice);
        var realizedPnl = grossPnl - p.EntryFee - exitFee;
        var closedAtUtc = DateTime.UtcNow;

        var closed = await store.TryCloseAsync(
            p.PositionId,
            p.Version,
            exitPrice,
            exitFee,
            realizedPnl,
            reason,
            closedAtUtc,
            ct);

        if (!closed)
            return TradeExecutionResult.Failure("Paper p changed concurrently; close was not applied.");

        await TryRecordPositionClosedAsync(
            p,
            exitPrice,
            exitFee,
            grossPnl,
            realizedPnl,
            reason,
            closedAtUtc,
            ct);

        await TryRecordPositionEventAsync(
            p, 
            TradingEventTypes.PositionClosed,
            new
            {
                p.ShortId,
                p.Symbol,
                Side = p.Side.ToString(),
                p.Quantity,
                ExitPrice = exitPrice,
                ExitFee = exitFee,
                GrossPnl = grossPnl,
                RealizedPnl = realizedPnl,
                Reason = reason,
                p.Source
            },
            closedAtUtc,
            ct);

        return TradeExecutionResult.Success(p.ShortId, reason);
    }


    private async Task TryRecordPositionEventAsync(PaperTradingPosition p, string eventType, object payload, DateTime occurredAtUtc, CancellationToken ct)
    {
        try
        {
            await eventStore.AppendAsync(
                new AppendTradingEvent(
                    EventType: eventType,
                    AggregateType: "PaperPosition",
                    AggregateId: p.ShortId,
                    Payload: payload,
                    OccurredAtUtc: occurredAtUtc,
                    BotName: p.BotName,
                    Symbol: p.Symbol,
                    PositionId: p.ShortId,
                    SignalId: p.SignalId,
                    CorrelationId: p.SignalId ?? p.ShortId,
                    CausationId: p.SignalId,
                    Actor: p.Source),
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Paper p domain event could not be persisted. EventType = {EventType}, Bot = {Bot}, Position = {Position}", eventType, p.BotName, p.ShortId);
        }
    }

    private async Task TryRecordPositionOpenedAsync(PaperTradingPosition p, CancellationToken ct)
    {
        try
        {
            await tradingPipelineRecorder.UpsertPositionAsync(CreateOpenHistoryRecord(p), ct);

            await tradingPipelineRecorder.RecordPositionEventAsync(
                new PositionEventHistoryRecord(
                    PositionId: ToHistoryPositionId(p.PositionId),
                    BotName: p.BotName,
                    EventType: "PAPER_POSITION_OPENED",
                    Status: "Open",
                    OccurredAtUtc: p.OpenedAtUtc,
                    Price: p.EntryPrice,
                    Quantity: p.Quantity,
                    Details: new Dictionary<string, object?>
                    {
                        ["shortId"] = p.ShortId,
                        ["symbol"] = p.Symbol,
                        ["side"] = p.Side.ToString(),
                        ["takeProfitPrice"] = p.TakeProfitPrice,
                        ["stopLossPrice"] = p.StopLossPrice,
                        ["entryFee"] = p.EntryFee,
                        ["source"] = p.Source
                    }),
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Paper p was opened, but its tradingPipelineRecorder record could not be persisted. Bot = {Bot}, Position = {Position}", p.BotName, p.ShortId);
        }
    }

    private async Task TryRecordPositionClosedAsync(
        PaperTradingPosition position,
        decimal exitPrice,
        decimal exitFee,
        decimal grossPnl,
        decimal realizedPnl,
        string reason,
        DateTime closedAtUtc,
        CancellationToken ct)
    {
        try
        {
            await tradingPipelineRecorder.UpsertPositionAsync(
                CreateClosedHistoryRecord(
                    position,
                    exitPrice,
                    exitFee,
                    grossPnl,
                    realizedPnl,
                    reason,
                    closedAtUtc),
                ct);

            await tradingPipelineRecorder.RecordPositionEventAsync(
                new PositionEventHistoryRecord(
                    PositionId: ToHistoryPositionId(position.PositionId),
                    BotName: position.BotName,
                    EventType: "PAPER_POSITION_CLOSED",
                    Status: "Closed",
                    OccurredAtUtc: closedAtUtc,
                    Price: exitPrice,
                    Quantity: position.Quantity,
                    Details: new Dictionary<string, object?>
                    {
                        ["shortId"] = position.ShortId,
                        ["symbol"] = position.Symbol,
                        ["side"] = position.Side.ToString(),
                        ["reason"] = reason,
                        ["grossPnl"] = grossPnl,
                        ["realizedPnl"] = realizedPnl,
                        ["entryFee"] = position.EntryFee,
                        ["exitFee"] = exitFee,
                        ["totalFees"] = position.EntryFee + exitFee,
                        ["source"] = position.Source
                    }),
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Paper p was closed, but its tradingPipelineRecorder record could not be persisted. Bot = {Bot}, Position = {Position}", position.BotName,  position.ShortId);
        }
    }

    private static PositionHistoryRecord CreateOpenHistoryRecord(PaperTradingPosition p)
    {
        return new PositionHistoryRecord(
            PositionId: ToHistoryPositionId(p.PositionId),
            SignalId: p.SignalId,
            BotName: p.BotName,
            StrategyVersion: p.StrategyVersion,
            Symbol: p.Symbol,
            Side: p.Side.ToString(),
            Source: p.Source,
            Environment: PaperEnvironment,
            Status: "Open",
            Quantity: p.Quantity,
            EntryPrice: p.EntryPrice,
            TakeProfitPrice: p.TakeProfitPrice,
            OpenedAtUtc: p.OpenedAtUtc,
            ClosedAtUtc: null,
            RealizedPnl: null,
            Fees: p.EntryFee,
            CloseReason: null,
            Metadata: new Dictionary<string, object?>
            {
                ["shortId"] = p.ShortId,
                ["stopLossPrice"] = p.StopLossPrice,
                ["entryFee"] = p.EntryFee
            });
    }

    private static PositionHistoryRecord CreateClosedHistoryRecord(
        PaperTradingPosition position,
        decimal exitPrice,
        decimal exitFee,
        decimal grossPnl,
        decimal realizedPnl,
        string reason,
        DateTime closedAtUtc)
    {
        return new PositionHistoryRecord(
            PositionId: ToHistoryPositionId(position.PositionId),
            SignalId: position.SignalId,
            BotName: position.BotName,
            StrategyVersion: position.StrategyVersion,
            Symbol: position.Symbol,
            Side: position.Side.ToString(),
            Source: position.Source,
            Environment: PaperEnvironment,
            Status: "Closed",
            Quantity: position.Quantity,
            EntryPrice: position.EntryPrice,
            TakeProfitPrice: position.TakeProfitPrice,
            OpenedAtUtc: position.OpenedAtUtc,
            ClosedAtUtc: closedAtUtc,
            RealizedPnl: realizedPnl,
            Fees: position.EntryFee + exitFee,
            CloseReason: reason,
            Metadata: new Dictionary<string, object?>
            {
                ["shortId"] = position.ShortId,
                ["stopLossPrice"] = position.StopLossPrice,
                ["exitPrice"] = exitPrice,
                ["entryFee"] = position.EntryFee,
                ["exitFee"] = exitFee,
                ["grossPnl"] = grossPnl
            });
    }

    private static TradeExecutionResult? ValidatePositionForClose(PaperTradingPosition? position, string shortId)
    {
        if (position is null)
            return TradeExecutionResult.Failure($"Paper p {shortId} was not found.");

        if (position.Status == PaperPositionStatus.Closed)
            return TradeExecutionResult.Success(shortId, "Paper p is already closed.");

        if (position.Status != PaperPositionStatus.Open)
            return TradeExecutionResult.Failure($"Paper p {shortId} is not open.");

        if (position.Quantity <= 0)
            return TradeExecutionResult.Failure($"Paper p {shortId} has invalid quantity.");

        return null;
    }

    private decimal ResolveTakeProfitPrice(decimal entryPrice, PositionSide side, decimal? takeProfitDistance)
    {
        if (takeProfitDistance is > 0)
            return side == PositionSide.Long ? entryPrice + takeProfitDistance.Value : entryPrice - takeProfitDistance.Value;

        return ApplyPercent(entryPrice, side, _options.DefaultTakeProfitPercent, favorable: true);
    }

    internal decimal ApplySlippage(decimal price, PositionSide side, bool opening)
    {
        var slippageRate = _options.SlippagePercent / 100m;
        var isBuyOperation = opening ? side == PositionSide.Long : side == PositionSide.Short;

        return isBuyOperation ? price * (1m + slippageRate) : price * (1m - slippageRate);
    }

    internal decimal CalculateFee(decimal price, decimal quantity)
        => price * quantity * (_options.CommissionPercent / 100m);

    internal static decimal CalculateGrossPnl(PaperTradingPosition position, decimal exitPrice)
        => position.Side == PositionSide.Long
            ? (exitPrice - position.EntryPrice) * position.Quantity
            : (position.EntryPrice - exitPrice) * position.Quantity;

    private static decimal ApplyPercent(decimal price, PositionSide side, decimal percent, bool favorable)
    {
        var change = price * percent / 100m;
        var shouldIncrease = favorable ? side == PositionSide.Long : side == PositionSide.Short;

        return shouldIncrease ? price + change : price - change;
    }

    private static string NormalizeSource(string? source)
        => string.IsNullOrWhiteSpace(source) ? "internal" : source;

    private static string CreateShortId(DateTime openedAtUtc)
        => $"P{openedAtUtc:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

    private static string ToHistoryPositionId(Guid positionId)
        => positionId.ToString("N");
}
