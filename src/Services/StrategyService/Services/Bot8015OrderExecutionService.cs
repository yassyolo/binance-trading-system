using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8015OrderExecutionService(
    IOptions<Bot8015Options> options,
    IBinanceFuturesOrderClient orders,
    ILogger<Bot8015OrderExecutionService> logger)
{
    private readonly Bot8015Options _options = options.Value;

    public async Task<BotPosition> OpenAsync(
        PositionSide side,
        string? source,
        CancellationToken cancellationToken)
    {
        var shortId = Guid.NewGuid().ToString("N")[..8];

        var parentClientId = CreateClientId("P", shortId);
        var tpClientId = CreateClientId("TP", shortId);
        var slClientId = CreateClientId("SL", shortId);

        var parent = await orders.PlaceMarketOrderAsync(
            _options.Symbol,
            ToEntrySide(side),
            ToPositionSide(side),
            _options.Quantity,
            parentClientId,
            cancellationToken);

        var filledParent = await WaitForFilledAsync(
            parent.OrderId,
            cancellationToken);

        var entryPrice = ResolveEntryPrice(filledParent);

        if (entryPrice <= 0)
        {
            throw new InvalidOperationException(
                $"BOT8015 parent order filled without a valid entry price. ShortId={shortId}, OrderId={parent.OrderId}");
        }

        var filters = await orders.GetSymbolFiltersAsync(
            _options.Symbol,
            cancellationToken);

        var tpRaw = side == PositionSide.Long
            ? entryPrice * (1m + (_options.TpPercent / 100m))
            : entryPrice * (1m - (_options.TpPercent / 100m));

        var slRaw = side == PositionSide.Long
            ? entryPrice - _options.InitialStopLoss
            : entryPrice + _options.InitialStopLoss;

        var tpPrice = QuantizeDown(tpRaw, filters.TickSize);
        var slPrice = QuantizeDown(slRaw, filters.TickSize);
        var tpQuantity = _options.Quantity / 2m;

        var tp = await orders.PlaceLimitOrderAsync(
            _options.Symbol,
            ToCloseSide(side),
            ToPositionSide(side),
            tpQuantity,
            tpPrice,
            tpClientId,
            cancellationToken);

        try
        {
            var sl = await orders.PlaceStopMarketAlgoOrderAsync(
                _options.Symbol,
                ToCloseSide(side),
                ToPositionSide(side),
                _options.Quantity,
                slPrice,
                slClientId,
                cancellationToken);

            logger.LogInformation(
                "BOT8015 opened. ShortId={ShortId}, Side={Side}, Entry={Entry}, TP={Tp}, SL={Sl}",
                shortId,
                side,
                entryPrice,
                tpPrice,
                slPrice);

            return new BotPosition
            {
                ShortId = shortId,
                BotName = _options.BotName,
                Symbol = _options.Symbol,
                Side = side,
                Mode = PositionMode.Stop3,

                EntryPrice = entryPrice,
                Quantity = _options.Quantity,
                RemainingQuantity = _options.Quantity,

                ParentClientId = parentClientId,
                ParentOrderId = filledParent.OrderId,
                ParentFilledAtUtc = DateTime.UtcNow,

                TpClientId = tpClientId,
                TpOrderId = tp.OrderId,
                TpPrice = tpPrice,
                TpStatus = tp.Status,
                TpExecuted = false,

                SlClientId = slClientId,
                SlOrderId = sl.AlgoOrderId,
                SlPrice = slPrice,
                SlStatus = sl.Status,
                SlExecuted = false,

                Stop3Created = false,
                Stop3Pending = false,
                ProtectiveActive = true,
                Closed = false,
                Status = PositionStatus.Open,
                Source = string.IsNullOrWhiteSpace(source)
                    ? "webhook"
                    : source.Trim()
            };
        }
        catch
        {
            await TryCancelNormalOrderAsync(
                tp.OrderId,
                cancellationToken);

            throw;
        }
    }

    private async Task<BinanceOrderResult> WaitForFilledAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);

        while (DateTime.UtcNow < deadline)
        {
            var order = await orders.GetOrderAsync(
                _options.Symbol,
                orderId,
                cancellationToken);

            if (order.Status?.Equals(
                    "FILLED",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                return order;
            }

            if (order.Status is "CANCELED" or "EXPIRED" or "REJECTED")
            {
                throw new InvalidOperationException(
                    $"BOT8015 parent order became terminal before FILLED. OrderId={orderId}, Status={order.Status}");
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(250),
                cancellationToken);
        }

        throw new TimeoutException(
            $"BOT8015 parent order was not FILLED within 20 seconds. OrderId={orderId}");
    }

    private async Task TryCancelNormalOrderAsync(
        string? orderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return;

        try
        {
            await orders.CancelOrderAsync(
                _options.Symbol,
                orderId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "BOT8015 failed to roll back TP after SL creation failure. OrderId={OrderId}",
                orderId);
        }
    }

    private static decimal ResolveEntryPrice(BinanceOrderResult order)
    {
        if (order.AveragePrice is > 0)
            return order.AveragePrice.Value;

        if (order.CumulativeQuoteQuantity is > 0
            && order.ExecutedQuantity is > 0)
        {
            return order.CumulativeQuoteQuantity.Value
                   / order.ExecutedQuantity.Value;
        }

        return 0m;
    }

    private static decimal QuantizeDown(decimal value, decimal tickSize)
    {
        if (tickSize <= 0)
            return value;

        return Math.Floor(value / tickSize) * tickSize;
    }

    private string CreateClientId(string type, string shortId)
    {
        var shortBot = _options.BotName.Length > 8
            ? _options.BotName[..8]
            : _options.BotName;

        var clientId = $"{shortBot}_{type}_{shortId}";

        return clientId.Length <= 32
            ? clientId
            : clientId[..32];
    }

    private static string ToEntrySide(PositionSide side)
        => side == PositionSide.Long ? "BUY" : "SELL";

    private static string ToCloseSide(PositionSide side)
        => side == PositionSide.Long ? "SELL" : "BUY";

    private static string ToPositionSide(PositionSide side)
        => side == PositionSide.Long ? "LONG" : "SHORT";
}
