using Microsoft.Extensions.Logging;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Exceptions;
using TradingSystem.Binance.Execution.Models;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Orders.Models;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace TradingSystem.Binance.Execution;

public sealed class BinanceTpOnlyPositionService(
    IBinanceFuturesOrderClient ordersClient,
    SafeBinanceOrderService safeOrders,
    IClock clock,
    ILogger<BinanceTpOnlyPositionService> logger)
{
    public async Task<BotPosition> OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        decimal quantity,
        decimal profitDistance,
        CancellationToken ct)
    {
        var id = BinanceClientOrderId.NewShortId();
        var parentId = BinanceClientOrderId.Create(botName, "P", id);
        var tpId = BinanceClientOrderId.Create(botName, "TP", id);

        var parentOrder = await safeOrders.SafePlaceMarketOrderAsync(
            symbol,
            BinanceOrderSide.Entry(side),
            BinanceOrderSide.Position(side),
            quantity,
            parentId,
            ct);

        var filled = await safeOrders.WaitForFillAsync(symbol, parentOrder, parentId, ct);

        var entry = ResolvePrice(filled);
        if (entry <= 0)
            throw new InvalidOperationException($"Filled order '{filled.OrderId}' has no valid price.");

        var filters = await ordersClient.GetSymbolFiltersAsync(symbol, ct);
        var raw = side == PositionSide.Long
            ? entry + profitDistance
            : entry - profitDistance;
        var tpPrice = Quantize(raw, filters.TickSize);

        var tp = await ordersClient.PlaceLimitOrderAsync(
            symbol,
            BinanceOrderSide.Close(side),
            BinanceOrderSide.Position(side),
            quantity,
            tpPrice,
            tpId,
            ct);

        var now = clock.UtcNow;

        logger.LogInformation("TP-only p opened. Bot = {Bot} Id = {Id} Side = {Side} Entry = {Entry} TP = {TP}", botName, id, side, entry, tpPrice);

        return new BotPosition
        {
            ShortId = id,
            BotName = botName,
            Symbol = symbol,
            Side = side,
            Mode = PositionMode.TpOnly,
            Quantity = quantity,
            RemainingQuantity = quantity,
            EntryPrice = entry,
            ParentClientId = parentId,
            ParentOrderId = filled.OrderId,
            ParentFilledAtUtc = now,
            TpClientId = tpId,
            TpOrderId = tp.OrderId,
            TpPrice = tpPrice,
            TpStatus = tp.Status,
            ProtectiveActive = true,
            Status = PositionStatus.Open,
            Source = "binance",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public async Task CloseAsync(BotPosition p, CancellationToken ct)
    {
        p.MarkClosing(clock.UtcNow);

        if (!p.TpExecuted && !string.IsNullOrWhiteSpace(p.TpOrderId))
        {
            await TryCancelOrderAsync(p.Symbol, p.TpOrderId, ct);
            p.TpStatus = "CANCELED";
        }

        if (p.RemainingQuantity <= 0)
            return;

        var clientOrderId = BinanceClientOrderId.Create(p.BotName, "CL", p.ShortId);

        var close = await safeOrders.SafePlaceMarketOrderAsync(
            p.Symbol,
            BinanceOrderSide.Close(p.Side),
            BinanceOrderSide.Position(p.Side),
            p.RemainingQuantity,
            clientOrderId,
            ct);

        p.CloseClientId = clientOrderId;
        p.CloseOrderId = close.OrderId;
        p.CloseStatus = close.Status;
    }

    private async Task TryCancelOrderAsync(string symbol, string orderId, CancellationToken ct)
    {
        try
        {
            await ordersClient.CancelOrderAsync(symbol, orderId, ct);
        }
        catch (BinanceApiException ex) when (IsUnknownOrder(ex))
        {
            logger.LogInformation("Standard Binance order was already absent while cancelling. Symbol = {Symbol} OrderId = {OrderId}", symbol, orderId);
        }
    }

    private static bool IsUnknownOrder(BinanceApiException exception)
        => exception.ResponseBody.Contains("\"code\":-2011", StringComparison.Ordinal);

    private static decimal ResolvePrice(BinanceOrderResult order)
        => order.AveragePrice is > 0
            ? order.AveragePrice.Value
            : order.CumulativeQuoteQuantity is > 0 && order.ExecutedQuantity is > 0
                ? order.CumulativeQuoteQuantity.Value / order.ExecutedQuantity.Value
                : 0;

    private static decimal Quantize(decimal value, decimal step)
        => step <= 0 ? value : Math.Floor(value / step) * step;
}
