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
    IBinanceFuturesOrderClient orders,
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

        var parent = await safeOrders.SafePlaceMarketOrderAsync(
            symbol,
            BinanceOrderSide.Entry(side),
            BinanceOrderSide.Position(side),
            quantity,
            parentId,
            ct);

        var filled = await safeOrders.WaitForFillAsync(
            symbol,
            parent,
            parentId,
            ct);

        var entry = ResolvePrice(filled);

        if (entry <= 0)
            throw new InvalidOperationException($"Filled order '{filled.OrderId}' has no valid price.");

        var filters = await orders.GetSymbolFiltersAsync(symbol, ct);
        var raw = side == PositionSide.Long
            ? entry + profitDistance
            : entry - profitDistance;
        var tpPrice = Quantize(raw, filters.TickSize);

        var tp = await orders.PlaceLimitOrderAsync(
            symbol,
            BinanceOrderSide.Close(side),
            BinanceOrderSide.Position(side),
            quantity,
            tpPrice,
            tpId,
            ct);

        var now = clock.UtcNow;

        logger.LogInformation(
            "TP-only position opened. Bot = {Bot} Id = {Id} Side = {Side} Entry = {Entry} TP = {TP}",
            botName,
            id,
            side,
            entry,
            tpPrice);

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

    public async Task CloseAsync(BotPosition position, CancellationToken ct)
    {
        position.MarkClosing(clock.UtcNow);

        if (!position.TpExecuted && !string.IsNullOrWhiteSpace(position.TpOrderId))
        {
            await TryCancelOrderAsync(position.Symbol, position.TpOrderId, ct);
            position.TpStatus = "CANCELED";
        }

        if (position.RemainingQuantity <= 0)
            return;

        var clientOrderId = BinanceClientOrderId.Create(
            position.BotName,
            "CL",
            position.ShortId);

        var close = await safeOrders.SafePlaceMarketOrderAsync(
            position.Symbol,
            BinanceOrderSide.Close(position.Side),
            BinanceOrderSide.Position(position.Side),
            position.RemainingQuantity,
            clientOrderId,
            ct);

        position.CloseClientId = clientOrderId;
        position.CloseOrderId = close.OrderId;
        position.CloseStatus = close.Status;
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
