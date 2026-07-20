using Microsoft.Extensions.Logging;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Exchange;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace TradingSystem.Binance.Execution;

public sealed class BinanceProtectedPositionService(
    IBinanceFuturesOrderClient orders, 
    BinanceExchangeInfoService exchange, 
    IClock clock, 
    ILogger<BinanceProtectedPositionService> logger)
{
    public async Task<BotPosition> OpenAsync(string bot, string symbol, PositionSide side, decimal quantity, decimal tpPercent, decimal stopDistance, CancellationToken ct)
    {
        var id = BinanceClientOrderId.NewShortId();
        var pId = BinanceClientOrderId.Create(bot, "P", id);
        var tpId = BinanceClientOrderId.Create(bot, "TP", id);
        var slId = BinanceClientOrderId.Create(bot, "SL", id);
        var parent = await orders.PlaceMarketOrderAsync(symbol, BinanceOrderSide.Entry(side), BinanceOrderSide.Position(side), quantity, pId, ct);
        var filled = await WaitAsync(symbol, parent.OrderId, ct);
        var entry = Price(filled);
        if(entry<=0)
            throw new InvalidOperationException("Entry order has no fill price.");
        var tpRaw = side==PositionSide.Long?entry*(1+tpPercent/100m):entry*(1-tpPercent/100m);var slRaw = side==PositionSide.Long?entry-stopDistance:entry+stopDistance;var tp = await exchange.RoundPriceAsync(symbol, tpRaw, ct);var sl = await exchange.RoundPriceAsync(symbol, slRaw, ct);var tpQty = await exchange.RoundQuantityAsync(symbol, quantity/2m, ct);if(tpQty<=0)throw new InvalidOperationException("Partial TP quantity is below Binance minimum.");var tpOrder = await orders.PlaceLimitOrderAsync(symbol, BinanceOrderSide.Close(side), BinanceOrderSide.Position(side), tpQty, tp, tpId, ct);try{var slOrder = await orders.PlaceStopMarketAlgoOrderAsync(symbol, BinanceOrderSide.Close(side), BinanceOrderSide.Position(side), quantity, sl, slId, ct);var now = clock.UtcNow;return new BotPosition{ShortId = id, BotName = bot, Symbol = symbol, Side = side, Mode = PositionMode.Stop3, Quantity = quantity, RemainingQuantity = quantity, EntryPrice = entry, ParentClientId = pId, ParentOrderId = filled.OrderId, ParentFilledAtUtc = now, TpClientId = tpId, TpOrderId = tpOrder.OrderId, TpPrice = tp, TpStatus = tpOrder.Status, SlClientId = slId, SlOrderId = slOrder.AlgoOrderId, SlPrice = sl, SlStatus = slOrder.Status, ProtectiveActive = true, Status = PositionStatus.Open, Source = "binance", CreatedAtUtc = now, UpdatedAtUtc = now};}catch{await orders.CancelOrderAsync(symbol, tpOrder.OrderId, ct);throw;}}
 public async Task CloseAsync(BotPosition p, CancellationToken ct)
    {
        p.MarkClosing(clock.UtcNow);
        if(!p.TpExecuted && !string.IsNullOrWhiteSpace(p.TpOrderId))
            await orders.CancelOrderAsync(p.Symbol, p.TpOrderId, ct);
        if(!p.SlExecuted && !string.IsNullOrWhiteSpace(p.SlOrderId))
            await orders.CancelAlgoOrderAsync(p.Symbol, p.SlOrderId, ct);
        if(!string.IsNullOrWhiteSpace(p.Stop3OrderId) && p.Stop3OrderId! = p.SlOrderId)
            await orders.CancelAlgoOrderAsync(p.Symbol, p.Stop3OrderId, ct);
        if(p.RemainingQuantity<=0)
            return;
        var cid = BinanceClientOrderId.Create(p.BotName, "CL", p.ShortId);
        var o = await orders.PlaceMarketOrderAsync(p.Symbol, BinanceOrderSide.Close(p.Side), BinanceOrderSide.Position(p.Side), p.RemainingQuantity, cid, ct);p.CloseClientId = cid;p.CloseOrderId = o.OrderId;p.CloseStatus = o.Status;logger.LogInformation("Protected position close submitted. Bot = {Bot} Position = {Position}", p.BotName, p.ShortId);}
 async Task<BinanceOrderResult> WaitAsync(string s, string id, CancellationToken ct)
    {
        var until = clock.UtcNow.AddSeconds(20);
        while(clock.UtcNow<until)
        {
            var o = await orders.GetOrderAsync(s, id, ct);
            if(o.Status?.Equals("FILLED", StringComparison.OrdinalIgnoreCase)==true)
                return o;
            if(o.Status is "CANCELED" or "EXPIRED" or "REJECTED")
                throw new InvalidOperationException($"Entry order terminal: {o.Status}");
            await Task.Delay(250, ct);
        }
        throw new TimeoutException($"Order {id} was not filled.");
    }
    static decimal Price(BinanceOrderResult x) => x.AveragePrice is>0?x.AveragePrice.Value:x.CumulativeQuoteQuantity is>0 && x.ExecutedQuantity is>0?x.CumulativeQuoteQuantity.Value/x.ExecutedQuantity.Value:0;}
