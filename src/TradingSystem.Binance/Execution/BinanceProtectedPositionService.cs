using Microsoft.Extensions.Logging;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Exceptions;
using TradingSystem.Binance.Exchange;
using TradingSystem.Binance.Execution.Models;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Orders.Models;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace TradingSystem.Binance.Execution;

public sealed class BinanceProtectedPositionService(
    IBinanceFuturesOrderClient orders,
    SafeBinanceOrderService safeOrders,
    BinanceExchangeInfoService exchange,
    IClock clock,
    ILogger<BinanceProtectedPositionService> logger)
{
    public async Task<BotPosition> OpenAsync(
        string bot,
        string symbol,
        PositionSide side,
        decimal quantity,
        decimal tpPercent,
        decimal stopDistance,
        CancellationToken ct)
    {
        var id = BinanceClientOrderId.NewShortId();
        var pId = BinanceClientOrderId.Create(bot, "P", id);
        var tpId = BinanceClientOrderId.Create(bot, "TP", id);
        var slId = BinanceClientOrderId.Create(bot, "SL", id);

        var parent = await safeOrders.SafePlaceMarketOrderAsync(
            symbol,
            BinanceOrderSide.Entry(side),
            BinanceOrderSide.Position(side),
            quantity,
            pId,
            ct);

        var filled = await safeOrders.WaitForFillAsync(
            symbol,
            parent,
            pId,
            ct);

        var entry = Price(filled);

        if (entry <= 0)
            throw new InvalidOperationException("Entry order has no fill price.");

        var tpRaw = side == PositionSide.Long
            ? entry * (1 + tpPercent / 100m)
            : entry * (1 - tpPercent / 100m);

        var slRaw = side == PositionSide.Long
            ? entry - stopDistance
            : entry + stopDistance;

        var tp = await exchange.RoundPriceAsync(symbol, tpRaw, ct);
        var sl = await exchange.RoundPriceAsync(symbol, slRaw, ct);
        var tpQty = await exchange.RoundQuantityAsync(symbol, quantity / 2m, ct);

        if (tpQty <= 0)
            throw new InvalidOperationException("Partial TP quantity is below Binance minimum.");

        var tpOrder = await orders.PlaceLimitOrderAsync(
            symbol,
            BinanceOrderSide.Close(side),
            BinanceOrderSide.Position(side),
            tpQty,
            tp,
            tpId,
            ct);

        try
        {
            var slOrder = await orders.PlaceStopMarketAlgoOrderAsync(
                symbol,
                BinanceOrderSide.Close(side),
                BinanceOrderSide.Position(side),
                quantity,
                sl,
                slId,
                ct);

            var now = clock.UtcNow;

            return new BotPosition
            {
                ShortId = id,
                BotName = bot,
                Symbol = symbol,
                Side = side,
                Mode = PositionMode.Stop3,
                Quantity = quantity,
                RemainingQuantity = quantity,
                EntryPrice = entry,
                ParentClientId = pId,
                ParentOrderId = filled.OrderId,
                ParentFilledAtUtc = now,
                TpClientId = tpId,
                TpOrderId = tpOrder.OrderId,
                TpPrice = tp,
                TpStatus = tpOrder.Status,
                SlClientId = slId,
                SlOrderId = slOrder.AlgoOrderId,
                SlPrice = sl,
                SlStatus = slOrder.Status,
                ProtectiveActive = true,
                Status = PositionStatus.Open,
                Source = "binance",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
        }
        catch
        {
            await TryCancelOrderAsync(symbol, tpOrder.OrderId, ct);
            throw;
        }
    }

    public async Task CloseAsync(BotPosition position, CancellationToken ct)
    {
        position.MarkClosing(clock.UtcNow);

        if (!position.TpExecuted && !string.IsNullOrWhiteSpace(position.TpOrderId))
            await TryCancelOrderAsync(position.Symbol, position.TpOrderId, ct);

        if (!position.SlExecuted && !string.IsNullOrWhiteSpace(position.SlOrderId))
            await TryCancelAlgoOrderAsync(position.Symbol, position.SlOrderId, ct);

        if (!string.IsNullOrWhiteSpace(position.Stop3OrderId) &&
            position.Stop3OrderId != position.SlOrderId)
        {
            await TryCancelAlgoOrderAsync(position.Symbol, position.Stop3OrderId, ct);
        }

        if (position.RemainingQuantity <= 0)
        {
            logger.LogInformation(
                "Protected position already has no remaining quantity. Bot = {Bot} Position = {Position}",
                position.BotName,
                position.ShortId);

            return;
        }

        var closeClientId = BinanceClientOrderId.Create(
            position.BotName,
            "CL",
            position.ShortId);

        var closeOrder = await safeOrders.SafePlaceMarketOrderAsync(
            position.Symbol,
            BinanceOrderSide.Close(position.Side),
            BinanceOrderSide.Position(position.Side),
            position.RemainingQuantity,
            closeClientId,
            ct);

        position.CloseClientId = closeClientId;
        position.CloseOrderId = closeOrder.OrderId;
        position.CloseStatus = closeOrder.Status;

        logger.LogInformation(
            "Protected position close submitted. Bot = {Bot} Position = {Position} Quantity = {Quantity}",
            position.BotName,
            position.ShortId,
            position.RemainingQuantity);
    }

    private async Task TryCancelOrderAsync(string symbol, string orderId, CancellationToken ct)
    {
        try
        {
            await orders.CancelOrderAsync(symbol, orderId, ct);
        }
        catch (BinanceApiException ex) when (IsUnknownOrder(ex))
        {
            logger.LogInformation(
                "Standard Binance order was already absent while cancelling. Symbol = {Symbol} OrderId = {OrderId}",
                symbol,
                orderId);
        }
    }

    private async Task TryCancelAlgoOrderAsync(string symbol, string orderId, CancellationToken ct)
    {
        try
        {
            await orders.CancelAlgoOrderAsync(symbol, orderId, ct);
        }
        catch (BinanceApiException ex) when (IsUnknownOrder(ex))
        {
            logger.LogInformation(
                "Binance algo order was already absent while cancelling. Symbol = {Symbol} OrderId = {OrderId}",
                symbol,
                orderId);
        }
    }

    private static bool IsUnknownOrder(BinanceApiException exception)
        => exception.ResponseBody.Contains("\"code\":-2011", StringComparison.Ordinal);

    private static decimal Price(BinanceOrderResult order)
        => order.AveragePrice is > 0
            ? order.AveragePrice.Value
            : order.CumulativeQuoteQuantity is > 0 && order.ExecutedQuantity is > 0
                ? order.CumulativeQuoteQuantity.Value / order.ExecutedQuantity.Value
                : 0;
}
