using Microsoft.Extensions.Logging;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;
namespace TradingSystem.Binance.Execution;
public sealed class BinanceTpOnlyPositionService(IBinanceFuturesOrderClient orders, IClock clock, ILogger<BinanceTpOnlyPositionService> logger)
{
 public async Task<BotPosition> OpenAsync(string botName, string symbol, PositionSide side, decimal quantity, decimal profitDistance, CancellationToken ct)
 {
  var id = BinanceClientOrderId.NewShortId(); var parentId = BinanceClientOrderId.Create(botName, "P", id); var tpId = BinanceClientOrderId.Create(botName, "TP", id);
  var parent = await orders.PlaceMarketOrderAsync(symbol, BinanceOrderSide.Entry(side), BinanceOrderSide.Position(side), quantity, parentId, ct);
  var filled = await WaitForFillAsync(symbol, parent.OrderId, ct); var entry = ResolvePrice(filled); if(entry<=0)throw new InvalidOperationException($"Filled order '{parent.OrderId}' has no valid price.");
  var filters = await orders.GetSymbolFiltersAsync(symbol, ct); var raw = side==PositionSide.Long?entry+profitDistance:entry-profitDistance; var tpPrice = Quantize(raw, filters.TickSize);
  var tp = await orders.PlaceLimitOrderAsync(symbol, BinanceOrderSide.Close(side), BinanceOrderSide.Position(side), quantity, tpPrice, tpId, ct); var now = clock.UtcNow;
  logger.LogInformation("TP-only position opened. Bot = {Bot} Id = {Id} Side = {Side} Entry = {Entry} TP = {TP}", botName, id, side, entry, tpPrice);
  return new BotPosition{ShortId = id, BotName = botName, Symbol = symbol, Side = side, Mode = PositionMode.TpOnly, Quantity = quantity, RemainingQuantity = quantity, EntryPrice = entry, ParentClientId = parentId, ParentOrderId = filled.OrderId, ParentFilledAtUtc = now, TpClientId = tpId, TpOrderId = tp.OrderId, TpPrice = tpPrice, TpStatus = tp.Status, ProtectiveActive = true, Status = PositionStatus.Open, Source = "binance", CreatedAtUtc = now, UpdatedAtUtc = now};
 }
 public async Task CloseAsync(BotPosition position, CancellationToken ct)
 {
  position.MarkClosing(clock.UtcNow); if(!position.TpExecuted && !string.IsNullOrWhiteSpace(position.TpOrderId)) await orders.CancelOrderAsync(position.Symbol, position.TpOrderId, ct);
  if(position.RemainingQuantity>0){var cid = BinanceClientOrderId.Create(position.BotName, "CL", position.ShortId);var close = await orders.PlaceMarketOrderAsync(position.Symbol, BinanceOrderSide.Close(position.Side), BinanceOrderSide.Position(position.Side), position.RemainingQuantity, cid, ct);position.CloseClientId = cid;position.CloseOrderId = close.OrderId;position.CloseStatus = close.Status;}
 }
 private async Task<BinanceOrderResult> WaitForFillAsync(string symbol, string id, CancellationToken ct){var deadline = clock.UtcNow.AddSeconds(20);while(clock.UtcNow<deadline){var x = await orders.GetOrderAsync(symbol, id, ct);if(x.Status?.Equals("FILLED", StringComparison.OrdinalIgnoreCase)==true)return x;if(x.Status is "CANCELED" or "EXPIRED" or "REJECTED")throw new InvalidOperationException($"Parent order terminal: {x.Status}");await Task.Delay(250, ct);}throw new TimeoutException($"Order '{id}' was not filled in 20 seconds.");}
 private static decimal ResolvePrice(BinanceOrderResult x) => x.AveragePrice is>0?x.AveragePrice.Value:x.CumulativeQuoteQuantity is>0 && x.ExecutedQuantity is>0?x.CumulativeQuoteQuantity.Value/x.ExecutedQuantity.Value:0;
 private static decimal Quantize(decimal value, decimal step) => step<=0?value:Math.Floor(value/step)*step;
}
