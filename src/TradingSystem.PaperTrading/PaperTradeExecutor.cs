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
    private readonly PaperTradingOptions _options  =  options.Value;

    public async Task<TradeExecutionResult> OpenAsync(string botName,  string symbol,  PositionSide side,  string? source,  CancellationToken ct)
    {
        if (!_options.Enabled)
            return TradeExecutionResult.Failure("Paper trading is disabled.");

        var open  =  await store.GetOpenAsync(ct);
        if (open.Count >= _options.MaximumOpenPositions)
            return TradeExecutionResult.Failure("Paper trading maximum open positions limit reached.");

        var configuration  =  await configurations.GetAsync(botName,  ct);
        if (configuration is null)
            return TradeExecutionResult.Failure($"Runtime configuration for {botName} was not found.");

        var markPrice  =  await prices.GetMarkPriceAsync(symbol,  ct);
        if (markPrice <= 0)
            return TradeExecutionResult.Failure($"Invalid mark price for {symbol}.");

        var entryPrice  =  ApplySlippage(markPrice,  side,  opening: true);
        var quantity  =  configuration.Quantity;
        var takeProfitDistance  =  configuration.ProfitDistance;
        var takeProfitPrice  =  takeProfitDistance is > 0
            ? side == PositionSide.Long ? entryPrice + takeProfitDistance.Value : entryPrice - takeProfitDistance.Value
            : ApplyPercent(entryPrice,  side,  _options.DefaultTakeProfitPercent,  favorable: true);
        var stopLossPrice  =  ApplyPercent(entryPrice,  side,  _options.DefaultStopLossPercent,  favorable: false);
        var fee  =  CalculateFee(entryPrice,  quantity);
        var shortId  =  $"P{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000,  9999)}";

        var position = new PaperTradingPosition
        {
            PositionId = Guid.NewGuid(),
            ShortId = shortId,
            BotName = botName,
            Symbol = symbol.ToUpperInvariant(),
            Side = side,
            Quantity = quantity,
            EntryPrice = entryPrice,
            TakeProfitPrice = takeProfitPrice,
            StopLossPrice = stopLossPrice,
            EntryFee = fee,
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

        await store.CreateAsync(position,  ct);
        return TradeExecutionResult.Success(shortId,  $"Paper position opened at {entryPrice}.");
    }

    public async Task<TradeExecutionResult> CloseAsync(string botName,  string shortId,  string reason,  CancellationToken ct)
    {
        var position  =  await store.GetAsync(botName,  shortId,  ct);
        if (position is null)
            return TradeExecutionResult.Failure($"Paper position {shortId} was not found.");
        if (position.Status == PaperPositionStatus.Closed)
            return TradeExecutionResult.Success(shortId,  "Paper position is already closed.");

        var markPrice  =  await prices.GetMarkPriceAsync(position.Symbol,  ct);
        var exitPrice  =  ApplySlippage(markPrice,  position.Side,  opening: false);
        var exitFee  =  CalculateFee(exitPrice,  position.Quantity);
        var pnl  =  CalculatePnl(position,  exitPrice,  exitFee);
        var closed  =  await store.TryCloseAsync(position.PositionId,  position.Version,  exitPrice,  exitFee,  pnl,  reason,  DateTime.UtcNow,  ct);
        return closed
            ? TradeExecutionResult.Success(shortId,  reason)
            : TradeExecutionResult.Failure("Paper position changed concurrently; close was not applied.");
    }

    internal decimal ApplySlippage(decimal price,  PositionSide side,  bool opening)
    {
        var pct  =  _options.SlippagePercent / 100m;
        var buy  =  opening ? side == PositionSide.Long : side == PositionSide.Short;
        return buy ? price * (1m + pct) : price * (1m - pct);
    }

    internal decimal CalculateFee(decimal price,  decimal quantity)  =>  price * quantity * (_options.CommissionPercent / 100m);

    internal static decimal CalculateGrossPnl(PaperTradingPosition p,  decimal exitPrice)  => 
        p.Side == PositionSide.Long
            ? (exitPrice - p.EntryPrice) * p.Quantity
            : (p.EntryPrice - exitPrice) * p.Quantity;

    private decimal CalculatePnl(PaperTradingPosition p,  decimal exitPrice,  decimal exitFee)  => 
        CalculateGrossPnl(p,  exitPrice) - p.EntryFee - exitFee;

    private static decimal ApplyPercent(decimal price,  PositionSide side,  decimal percent,  bool favorable)
    {
        var change  =  price * percent / 100m;
        var increase  =  favorable ? side == PositionSide.Long : side == PositionSide.Short;
        return increase ? price + change : price - change;
    }
}
