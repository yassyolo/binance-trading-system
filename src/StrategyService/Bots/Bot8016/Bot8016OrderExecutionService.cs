using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using StrategyService.Bots.Bot8016.Models;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Orders.Models;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016OrderExecutionService(
    IOptions<Bot8016Options> options, 
    IBinanceFuturesOrderClient orders)
{
    private readonly Bot8016Options _options  =  options.Value;

    public async Task<BotPosition> OpenAsync(Bot8016EntrySignal signal,  CancellationToken ct)
    {
        var shortId  =  Guid.NewGuid().ToString("N")[..8];
        var parentClientId  =  CreateClientId("P",  shortId);
        var tpClientId  =  CreateClientId("TP",  shortId);
        var slClientId  =  CreateClientId("SL",  shortId);

        var parent  =  await orders.PlaceMarketOrderAsync(
            _options.Symbol, 
            ToEntrySide(signal.Side), 
            ToPositionSide(signal.Side), 
            _options.Quantity, 
            parentClientId, 
            ct);

        var filled  =  await WaitForFilledAsync(parent.OrderId,  ct);

        var entryPrice  =  ResolveEntryPrice(filled);
        if (entryPrice <= 0)
            throw new InvalidOperationException($"BOT8016 invalid entry price. ShortId = {shortId}");

        var filters  =  await orders.GetSymbolFiltersAsync(_options.Symbol,  ct);

        var signalStop  =  signal.Side == PositionSide.Long
            ? signal.Candle.Low
            : signal.Candle.High;

        if (signalStop <= 0)
        {
            signalStop  =  signal.Side == PositionSide.Long
                ? entryPrice - _options.InitialStopLossFallback
                : entryPrice + _options.InitialStopLossFallback;
        }

        var rawTp  =  signal.Side == PositionSide.Long
            ? entryPrice * (1m + _options.TpPercent / 100m)
            : entryPrice * (1m - _options.TpPercent / 100m);

        var tpPrice  =  QuantizeDown(rawTp,  filters.TickSize);
        var slPrice  =  QuantizeDown(signalStop,  filters.TickSize);
        var tpQuantity  =  _options.Quantity / 2m;

        var tp  =  await orders.PlaceLimitOrderAsync(
            _options.Symbol, 
            ToCloseSide(signal.Side), 
            ToPositionSide(signal.Side), 
            tpQuantity, 
            tpPrice, 
            tpClientId, 
            ct);

        try
        {
            var sl  =  await orders.PlaceStopMarketAlgoOrderAsync(
                _options.Symbol, 
                ToCloseSide(signal.Side), 
                ToPositionSide(signal.Side), 
                _options.Quantity, 
                slPrice, 
                slClientId, 
                ct);

            return new BotPosition
            {
                ShortId  =  shortId, 
                BotName  =  _options.BotName, 
                Symbol  =  _options.Symbol, 
                Side  =  signal.Side, 
                Mode  =  PositionMode.Stop3, 

                EntryPrice  =  entryPrice, 
                Quantity  =  _options.Quantity, 
                RemainingQuantity  =  _options.Quantity, 

                ParentClientId  =  parentClientId, 
                ParentOrderId  =  filled.OrderId, 
                ParentFilledAtUtc  =  DateTime.UtcNow, 

                TpClientId  =  tpClientId, 
                TpOrderId  =  tp.OrderId, 
                TpPrice  =  tpPrice, 
                TpStatus  =  tp.Status, 

                SlClientId  =  slClientId, 
                SlOrderId  =  sl.AlgoOrderId, 
                SlPrice  =  slPrice, 
                SlStatus  =  sl.Status, 

                SignalCandleHigh  =  signal.Candle.High, 
                SignalCandleLow  =  signal.Candle.Low, 
                SignalCandleCloseTime  =  signal.Candle.CloseTime, 
                HighReached  =  false, 

                Stop3Created  =  false, 
                Stop3Pending  =  false, 
                ProtectiveActive  =  true, 
                Closed  =  false, 
                Status  =  PositionStatus.Open, 
                Source  =  "alligator"
            };
        }
        catch
        {
            await orders.CancelOrderAsync(_options.Symbol,  tp.OrderId,  ct);
            
            throw;
        }
    }

    public async Task CreateStop3AfterBreakoutAsync(BotPosition position,  CancellationToken ct)
    {
        if (position.RemainingQuantity <= 0)
            throw new InvalidOperationException("STOP3 requires remaining quantity.");

        var filters  =  await orders.GetSymbolFiltersAsync(position.Symbol,  ct);

        var rawTrigger  =  position.Side == PositionSide.Long
            ? position.EntryPrice + _options.Stop3EntryOffset
            : position.EntryPrice - _options.Stop3EntryOffset;

        var trigger  =  QuantizeDown(rawTrigger!.Value,  filters.TickSize);
        var clientId  =  CreateClientId("STOP3",  position.ShortId);

        var stop3  =  await orders.PlaceStopMarketAlgoOrderAsync(
            position.Symbol, 
            ToCloseSide(position.Side), 
            ToPositionSide(position.Side), 
            position.RemainingQuantity, 
            trigger, 
            clientId, 
            ct);

        position.Stop3ClientId  =  clientId;
        position.Stop3OrderId  =  stop3.AlgoOrderId;
        position.Stop3Initial  =  trigger;
        position.Stop3Current  =  trigger;
        position.Stop3Previous  =  trigger;
        position.Stop3Status  =  stop3.Status;
        position.Stop3Created  =  true;
        position.Stop3Pending  =  false;
        position.ProtectiveActive  =  true;
        position.HighReached  =  true;
    }

    private async Task<BinanceOrderResult> WaitForFilledAsync(string orderId,  CancellationToken ct)
    {
        var deadline  =  DateTime.UtcNow.AddSeconds(20);

        while (DateTime.UtcNow < deadline)
        {
            var order  =  await orders.GetOrderAsync(_options.Symbol,  orderId,  ct);

            if (order.Status?.Equals("FILLED",  StringComparison.OrdinalIgnoreCase) == true)
                return order;

            if (order.Status is "CANCELED" or "EXPIRED" or "REJECTED")
                throw new InvalidOperationException($"BOT8016 parent order terminal before fill. OrderId = {orderId},  Status = {order.Status}");

            await Task.Delay(250,  ct);
        }

        throw new TimeoutException($"BOT8016 parent order was not filled. OrderId = {orderId}");
    }

    private static decimal ResolveEntryPrice(BinanceOrderResult order)
    {
        if (order.AveragePrice is > 0)
            return order.AveragePrice.Value;

        if (order.CumulativeQuoteQuantity is > 0  &&  order.ExecutedQuantity is > 0)
            return order.CumulativeQuoteQuantity.Value / order.ExecutedQuantity.Value;

        return 0;
    }

    private static decimal QuantizeDown(decimal value,  decimal tickSize)
         =>  tickSize <= 0 ? value : Math.Floor(value / tickSize) * tickSize;

    private string CreateClientId(string type,  string shortId)
    {
        var bot  =  _options.BotName.Length > 8 ? _options.BotName[..8] : _options.BotName;
        var value  =  $"{bot}_{type}_{shortId}";
        
        return value.Length <= 32 ? value : value[..32];
    }

    private static string ToEntrySide(PositionSide side)
         => side == PositionSide.Long ? "BUY" : "SELL";

    private static string ToCloseSide(PositionSide side)
         => side == PositionSide.Long ? "SELL" : "BUY";

    private static string ToPositionSide(PositionSide side)
         => side == PositionSide.Long ? "LONG" : "SHORT";
}
