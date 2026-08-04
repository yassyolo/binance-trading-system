using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Execution;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.Domain.Enums;
using TradingSystem.Observability.History;
using TradingSystem.PaperTrading.Configuration;

namespace TradingSystem.PaperTrading.Executor;

public sealed class PaperTradeExecutor(
    IPaperTradingStore store,
    IMarketPriceProvider prices,
    IBotRuntimeConfigurationProvider configurations,
    ITradingPipelineRecorder history,
    IOptions<PaperTradingOptions> options,
    ILogger<PaperTradeExecutor> logger)
{
    private const string PaperEnvironment = "Paper";
    private const string UnknownStrategyVersion = "unknown";

    private readonly PaperTradingOptions _options = options.Value;

    public async Task<TradeExecutionResult> OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken ct)
    {
        if (!_options.Enabled)
            return TradeExecutionResult.Failure("Paper trading is disabled.");

        var openPositions = await store.GetOpenAsync(ct);
        if (openPositions.Count >= _options.MaximumOpenPositions)
            return TradeExecutionResult.Failure("Paper trading maximum open positions limit reached.");

        var configuration = await configurations.GetAsync(botName, ct);
        if (configuration is null)
            return TradeExecutionResult.Failure($"Runtime configuration for {botName} was not found.");

        if (configuration.Quantity <= 0)
            return TradeExecutionResult.Failure($"Invalid paper trading quantity configured for {botName}.");

        var normalizedSymbol = symbol.ToUpperInvariant();
        var markPrice = await prices.GetMarkPriceAsync(normalizedSymbol, ct);

        if (markPrice <= 0)
            return TradeExecutionResult.Failure($"Invalid mark price for {normalizedSymbol}.");

        var entryPrice = ApplySlippage(markPrice, side, opening: true);
        var quantity = configuration.Quantity;
        var takeProfitPrice = ResolveTakeProfitPrice(entryPrice, side, configuration.ProfitDistance);
        var stopLossPrice = ApplyPercent(entryPrice, side, _options.DefaultStopLossPercent, favorable: false);
        var entryFee = CalculateFee(entryPrice, quantity);
        var openedAtUtc = DateTime.UtcNow;

        var position = new PaperTradingPosition
        {
            PositionId = Guid.NewGuid(),
            ShortId = CreateShortId(openedAtUtc),
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

        return TradeExecutionResult.Success(
            position.ShortId,
            $"Paper position opened at {entryPrice}.");
    }

    public async Task<TradeExecutionResult> CloseAsync(
        string botName,
        string shortId,
        string reason,
        CancellationToken ct)
    {
        var position = await store.GetAsync(botName, shortId, ct);
        var validationResult = ValidatePositionForClose(position, shortId);

        if (validationResult is not null)
            return validationResult;

        var markPrice = await prices.GetMarkPriceAsync(position!.Symbol, ct);
        if (markPrice <= 0)
            return TradeExecutionResult.Failure($"Invalid mark price for {position.Symbol}.");

        return await ClosePositionAtPriceAsync(position, markPrice, reason, ct);
    }

    public async Task<TradeExecutionResult> CloseAtPriceAsync(
        string botName,
        string shortId,
        decimal triggerPrice,
        string reason,
        CancellationToken ct)
    {
        if (triggerPrice <= 0)
            return TradeExecutionResult.Failure("Paper position trigger price must be positive.");

        var position = await store.GetAsync(botName, shortId, ct);
        var validationResult = ValidatePositionForClose(position, shortId);

        if (validationResult is not null)
            return validationResult;

        return await ClosePositionAtPriceAsync(position!, triggerPrice, reason, ct);
    }

    private async Task<TradeExecutionResult> ClosePositionAtPriceAsync(
        PaperTradingPosition position,
        decimal marketPrice,
        string reason,
        CancellationToken ct)
    {
        var exitPrice = ApplySlippage(marketPrice, position.Side, opening: false);
        var exitFee = CalculateFee(exitPrice, position.Quantity);
        var grossPnl = CalculateGrossPnl(position, exitPrice);
        var realizedPnl = grossPnl - position.EntryFee - exitFee;
        var closedAtUtc = DateTime.UtcNow;

        var closed = await store.TryCloseAsync(
            position.PositionId,
            position.Version,
            exitPrice,
            exitFee,
            realizedPnl,
            reason,
            closedAtUtc,
            ct);

        if (!closed)
            return TradeExecutionResult.Failure("Paper position changed concurrently; close was not applied.");

        await TryRecordPositionClosedAsync(
            position,
            exitPrice,
            exitFee,
            grossPnl,
            realizedPnl,
            reason,
            closedAtUtc,
            ct);

        return TradeExecutionResult.Success(position.ShortId, reason);
    }

    private async Task TryRecordPositionOpenedAsync(
        PaperTradingPosition position,
        CancellationToken ct)
    {
        try
        {
            await history.UpsertPositionAsync(CreateOpenHistoryRecord(position), ct);

            await history.RecordPositionEventAsync(
                new PositionEventHistoryRecord(
                    PositionId: ToHistoryPositionId(position.PositionId),
                    BotName: position.BotName,
                    EventType: "PAPER_POSITION_OPENED",
                    Status: "Open",
                    OccurredAtUtc: position.OpenedAtUtc,
                    Price: position.EntryPrice,
                    Quantity: position.Quantity,
                    Details: new Dictionary<string, object?>
                    {
                        ["shortId"] = position.ShortId,
                        ["symbol"] = position.Symbol,
                        ["side"] = position.Side.ToString(),
                        ["takeProfitPrice"] = position.TakeProfitPrice,
                        ["stopLossPrice"] = position.StopLossPrice,
                        ["entryFee"] = position.EntryFee,
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
            logger.LogError(
                exception,
                "Paper position was opened, but its history record could not be persisted. Bot = {Bot}, Position = {Position}",
                position.BotName,
                position.ShortId);
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
            await history.UpsertPositionAsync(
                CreateClosedHistoryRecord(
                    position,
                    exitPrice,
                    exitFee,
                    grossPnl,
                    realizedPnl,
                    reason,
                    closedAtUtc),
                ct);

            await history.RecordPositionEventAsync(
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
            logger.LogError(
                exception,
                "Paper position was closed, but its history record could not be persisted. Bot = {Bot}, Position = {Position}",
                position.BotName,
                position.ShortId);
        }
    }

    private static PositionHistoryRecord CreateOpenHistoryRecord(
        PaperTradingPosition position)
    {
        return new PositionHistoryRecord(
            PositionId: ToHistoryPositionId(position.PositionId),
            SignalId: null,
            BotName: position.BotName,
            StrategyVersion: UnknownStrategyVersion,
            Symbol: position.Symbol,
            Side: position.Side.ToString(),
            Source: position.Source,
            Environment: PaperEnvironment,
            Status: "Open",
            Quantity: position.Quantity,
            EntryPrice: position.EntryPrice,
            TakeProfitPrice: position.TakeProfitPrice,
            OpenedAtUtc: position.OpenedAtUtc,
            ClosedAtUtc: null,
            RealizedPnl: null,
            Fees: position.EntryFee,
            CloseReason: null,
            Metadata: new Dictionary<string, object?>
            {
                ["shortId"] = position.ShortId,
                ["stopLossPrice"] = position.StopLossPrice,
                ["entryFee"] = position.EntryFee
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
            SignalId: null,
            BotName: position.BotName,
            StrategyVersion: UnknownStrategyVersion,
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

    private static TradeExecutionResult? ValidatePositionForClose(
        PaperTradingPosition? position,
        string shortId)
    {
        if (position is null)
            return TradeExecutionResult.Failure($"Paper position {shortId} was not found.");

        if (position.Status == PaperPositionStatus.Closed)
            return TradeExecutionResult.Success(shortId, "Paper position is already closed.");

        if (position.Status != PaperPositionStatus.Open)
            return TradeExecutionResult.Failure($"Paper position {shortId} is not open.");

        if (position.Quantity <= 0)
            return TradeExecutionResult.Failure($"Paper position {shortId} has invalid quantity.");

        return null;
    }

    private decimal ResolveTakeProfitPrice(
        decimal entryPrice,
        PositionSide side,
        decimal? takeProfitDistance)
    {
        if (takeProfitDistance is > 0)
        {
            return side == PositionSide.Long
                ? entryPrice + takeProfitDistance.Value
                : entryPrice - takeProfitDistance.Value;
        }

        return ApplyPercent(entryPrice, side, _options.DefaultTakeProfitPercent, favorable: true);
    }

    internal decimal ApplySlippage(
        decimal price,
        PositionSide side,
        bool opening)
    {
        var slippageRate = _options.SlippagePercent / 100m;
        var isBuyOperation = opening
            ? side == PositionSide.Long
            : side == PositionSide.Short;

        return isBuyOperation
            ? price * (1m + slippageRate)
            : price * (1m - slippageRate);
    }

    internal decimal CalculateFee(
        decimal price,
        decimal quantity)
    {
        return price * quantity * (_options.CommissionPercent / 100m);
    }

    internal static decimal CalculateGrossPnl(
        PaperTradingPosition position,
        decimal exitPrice)
    {
        return position.Side == PositionSide.Long
            ? (exitPrice - position.EntryPrice) * position.Quantity
            : (position.EntryPrice - exitPrice) * position.Quantity;
    }

    private static decimal ApplyPercent(
        decimal price,
        PositionSide side,
        decimal percent,
        bool favorable)
    {
        var change = price * percent / 100m;
        var shouldIncrease = favorable
            ? side == PositionSide.Long
            : side == PositionSide.Short;

        return shouldIncrease
            ? price + change
            : price - change;
    }

    private static string NormalizeSource(string? source)
    {
        return string.IsNullOrWhiteSpace(source)
            ? "internal"
            : source;
    }

    private static string CreateShortId(DateTime openedAtUtc)
    {
        return $"P{openedAtUtc:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    private static string ToHistoryPositionId(Guid positionId)
    {
        return positionId.ToString("N");
    }
}
