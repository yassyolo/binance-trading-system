using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Execution;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.Domain.Enums;

namespace TradingSystem.PaperTrading;

public sealed class PaperTradeExecutor(
    IPaperTradingStore store,
    IMarketPriceProvider prices,
    IBotRuntimeConfigurationProvider configurations,
    IOptions<PaperTradingOptions> options)
{
    private readonly PaperTradingOptions _options = options.Value;

    public async Task<TradeExecutionResult> OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return TradeExecutionResult.Failure(
                "Paper trading is disabled.");
        }

        var openPositions = await store.GetOpenAsync(ct);

        if (openPositions.Count >= _options.MaximumOpenPositions)
        {
            return TradeExecutionResult.Failure(
                "Paper trading maximum open positions limit reached.");
        }

        var configuration = await configurations.GetAsync(
            botName,
            ct);

        if (configuration is null)
        {
            return TradeExecutionResult.Failure(
                $"Runtime configuration for {botName} was not found.");
        }

        if (configuration.Quantity <= 0)
        {
            return TradeExecutionResult.Failure(
                $"Invalid paper trading quantity configured for {botName}.");
        }

        var normalizedSymbol = symbol.ToUpperInvariant();

        var markPrice = await prices.GetMarkPriceAsync(
            normalizedSymbol,
            ct);

        if (markPrice <= 0)
        {
            return TradeExecutionResult.Failure(
                $"Invalid mark price for {normalizedSymbol}.");
        }

        var entryPrice = ApplySlippage(
            markPrice,
            side,
            opening: true);

        var quantity = configuration.Quantity;

        var takeProfitPrice = ResolveTakeProfitPrice(
            entryPrice,
            side,
            configuration.ProfitDistance);

        var stopLossPrice = ApplyPercent(
            entryPrice,
            side,
            _options.DefaultStopLossPercent,
            favorable: false);

        var entryFee = CalculateFee(
            entryPrice,
            quantity);

        var shortId =
            $"P{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

        var position = new PaperTradingPosition
        {
            PositionId = Guid.NewGuid(),
            ShortId = shortId,
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
            Source = string.IsNullOrWhiteSpace(source)
                ? "internal"
                : source,
            OpenedAtUtc = DateTime.UtcNow,
            ClosedAtUtc = null,
            CloseReason = null,
            Version = 1
        };

        await store.CreateAsync(
            position,
            ct);

        return TradeExecutionResult.Success(
            shortId,
            $"Paper position opened at {entryPrice}.");
    }

    /// <summary>
    /// Closes a paper position manually using the latest available mark price.
    /// </summary>
    public async Task<TradeExecutionResult> CloseAsync(
        string botName,
        string shortId,
        string reason,
        CancellationToken ct)
    {
        var position = await store.GetAsync(
            botName,
            shortId,
            ct);

        var validationResult = ValidatePositionForClose(
            position,
            shortId);

        if (validationResult is not null)
            return validationResult;

        var markPrice = await prices.GetMarkPriceAsync(
            position!.Symbol,
            ct);

        if (markPrice <= 0)
        {
            return TradeExecutionResult.Failure(
                $"Invalid mark price for {position.Symbol}.");
        }

        return await ClosePositionAtPriceAsync(
            position,
            markPrice,
            reason,
            ct);
    }

    /// <summary>
    /// Closes a paper position using a supplied trigger price.
    /// Intended for TP/SL workers that already obtained the market price
    /// used to detect the exit condition.
    /// </summary>
    public async Task<TradeExecutionResult> CloseAtPriceAsync(
        string botName,
        string shortId,
        decimal triggerPrice,
        string reason,
        CancellationToken ct)
    {
        if (triggerPrice <= 0)
        {
            return TradeExecutionResult.Failure(
                "Paper position trigger price must be positive.");
        }

        var position = await store.GetAsync(
            botName,
            shortId,
            ct);

        var validationResult = ValidatePositionForClose(
            position,
            shortId);

        if (validationResult is not null)
            return validationResult;

        return await ClosePositionAtPriceAsync(
            position!,
            triggerPrice,
            reason,
            ct);
    }

    private async Task<TradeExecutionResult> ClosePositionAtPriceAsync(
        PaperTradingPosition position,
        decimal marketPrice,
        string reason,
        CancellationToken ct)
    {
        var exitPrice = ApplySlippage(
            marketPrice,
            position.Side,
            opening: false);

        var exitFee = CalculateFee(
            exitPrice,
            position.Quantity);

        var realizedPnl = CalculatePnl(
            position,
            exitPrice,
            exitFee);

        var closed = await store.TryCloseAsync(
            position.PositionId,
            position.Version,
            exitPrice,
            exitFee,
            realizedPnl,
            reason,
            DateTime.UtcNow,
            ct);

        return closed
            ? TradeExecutionResult.Success(
                position.ShortId,
                reason)
            : TradeExecutionResult.Failure(
                "Paper position changed concurrently; close was not applied.");
    }

    private static TradeExecutionResult? ValidatePositionForClose(
        PaperTradingPosition? position,
        string shortId)
    {
        if (position is null)
        {
            return TradeExecutionResult.Failure(
                $"Paper position {shortId} was not found.");
        }

        if (position.Status == PaperPositionStatus.Closed)
        {
            return TradeExecutionResult.Success(
                shortId,
                "Paper position is already closed.");
        }

        if (position.Status != PaperPositionStatus.Open)
        {
            return TradeExecutionResult.Failure(
                $"Paper position {shortId} is not open.");
        }

        if (position.Quantity <= 0)
        {
            return TradeExecutionResult.Failure(
                $"Paper position {shortId} has invalid quantity.");
        }

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

        return ApplyPercent(
            entryPrice,
            side,
            _options.DefaultTakeProfitPercent,
            favorable: true);
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
        return price *
               quantity *
               (_options.CommissionPercent / 100m);
    }

    internal static decimal CalculateGrossPnl(
        PaperTradingPosition position,
        decimal exitPrice)
    {
        return position.Side == PositionSide.Long
            ? (exitPrice - position.EntryPrice) * position.Quantity
            : (position.EntryPrice - exitPrice) * position.Quantity;
    }

    private decimal CalculatePnl(
        PaperTradingPosition position,
        decimal exitPrice,
        decimal exitFee)
    {
        var grossPnl = CalculateGrossPnl(
            position,
            exitPrice);

        return grossPnl -
               position.EntryFee -
               exitFee;
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
}