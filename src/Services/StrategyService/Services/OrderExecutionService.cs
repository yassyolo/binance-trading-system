using StrategyService.Configuration;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class OrderExecutionService
{
    private readonly IBinanceFuturesOrderClient _binanceOrders;
    private readonly ILogger<OrderExecutionService> _logger;

    public OrderExecutionService(
        IBinanceFuturesOrderClient binanceOrders,
        ILogger<OrderExecutionService> logger)
    {
        _binanceOrders = binanceOrders;
        _logger = logger;
    }

    public async Task<BotPosition> OpenBot8011PositionAsync(
        PositionSide side,
        Bot8011Options options,
        CancellationToken cancellationToken = default)
    {
        var shortId = CreateShortId();
        var entryClientId = CreateClientId(options.BotName, "P", shortId);
        var tpClientId = CreateClientId(options.BotName, "TP", shortId);
        var stop3ClientId = CreateClientId(options.BotName, "S3", shortId);

        var entry = await _binanceOrders.PlaceMarketOrderAsync(
            options.Symbol,
            ToEntrySide(side),
            ToPositionSide(side),
            options.Quantity,
            entryClientId,
            cancellationToken);

        var entryPrice = entry.AveragePrice ?? 0;

        var tpPrice = CalculatePercentPrice(side, entryPrice, options.TakeProfitPercent, true);
        var stop3Price = CalculatePercentPrice(side, entryPrice, options.StopLossPercent, false);

        var tpQuantity = options.Quantity / 2m;

        var tp = await _binanceOrders.PlaceLimitOrderAsync(
            options.Symbol,
            ToCloseSide(side),
            ToPositionSide(side),
            tpQuantity,
            tpPrice,
            tpClientId,
            cancellationToken);

        var stop3 = await _binanceOrders.PlaceStopMarketAlgoOrderAsync(
            options.Symbol,
            ToCloseSide(side),
            ToPositionSide(side),
            options.Quantity,
            stop3Price,
            stop3ClientId,
            cancellationToken);

        return new BotPosition
        {
            ShortId = shortId,
            BotName = options.BotName,
            Symbol = options.Symbol,
            Side = side,
            Mode = PositionMode.Stop3,

            EntryPrice = entryPrice,
            Quantity = options.Quantity,
            RemainingQuantity = options.Quantity,

            ParentClientId = entryClientId,
            ParentOrderId = entry.OrderId,
            ParentFilledAtUtc = DateTime.UtcNow,

            TpPrice = tpPrice,
            TpClientId = tpClientId,

            Stop3Initial = stop3Price,
            Stop3Current = stop3Price,
            Stop3Previous = stop3Price,
            Stop3ClientId = stop3ClientId,
            Stop3OrderId = stop3.AlgoOrderId,
            Stop3Status = stop3.Status,

            TpOrderId = tp.OrderId,
            TpStatus = tp.Status,

            SlPrice = stop3Price,
            SlClientId = stop3ClientId,
            SlOrderId = stop3.AlgoOrderId,
            SlStatus = stop3.Status,

            Stop3Created = false,
            Stop3Pending = false,

            ProtectiveActive = true,
            Closed = false,
            Status = "OPEN",
            Source = "binance"
        };
    }

    public async Task<BotPosition> OpenTpOnlyPositionAsync(
        string botName,
        PositionSide side,
        string symbol,
        decimal quantity,
        decimal profitDistance,
        CancellationToken cancellationToken = default)
    {
        var shortId = CreateShortId();
        var entryClientId = CreateClientId(botName, "P", shortId);
        var tpClientId = CreateClientId(botName, "TP", shortId);

        var entry = await _binanceOrders.PlaceMarketOrderAsync(
            symbol,
            ToEntrySide(side),
            ToPositionSide(side),
            quantity,
            entryClientId,
            cancellationToken);

        var entryPrice = entry.AveragePrice ?? 0;

        var tpPrice = side == PositionSide.Long
            ? entryPrice + profitDistance
            : entryPrice - profitDistance;

        var tp = await _binanceOrders.PlaceTakeProfitMarketAlgoOrderAsync(
            symbol,
            ToCloseSide(side),
            ToPositionSide(side),
            quantity,
            tpPrice,
            tpClientId,
            cancellationToken);

        return new BotPosition
        {
            ShortId = shortId,
            BotName = botName,
            Symbol = symbol,
            Side = side,
            Mode = PositionMode.TpOnly,

            EntryPrice = entryPrice,
            Quantity = quantity,
            RemainingQuantity = quantity,

            ParentClientId = entryClientId,
            ParentOrderId = entry.OrderId,
            ParentFilledAtUtc = DateTime.UtcNow,

            TpPrice = tpPrice,
            TpClientId = tpClientId,
            TpOrderId = tp.AlgoOrderId,
            TpStatus = tp.Status,

            ProtectiveActive = true,
            Closed = false,
            Status = "OPEN",
            Source = "binance"
        };
    }

    public async Task<BotPosition> OpenStop3TrailingPositionAsync(
        string botName,
        PositionSide side,
        string symbol,
        decimal quantity,
        decimal initialStopLoss,
        decimal takeProfitPercent,
        CancellationToken cancellationToken = default)
    {
        var shortId = CreateShortId();
        var entryClientId = CreateClientId(botName, "P", shortId);
        var tpClientId = CreateClientId(botName, "TP", shortId);
        var stop3ClientId = CreateClientId(botName, "S3", shortId);

        var entry = await _binanceOrders.PlaceMarketOrderAsync(
            symbol,
            ToEntrySide(side),
            ToPositionSide(side),
            quantity,
            entryClientId,
            cancellationToken);

        var entryPrice = entry.AveragePrice ?? 0;

        var tpPrice = CalculatePercentPrice(side, entryPrice, takeProfitPercent, true);

        var stop3Price = side == PositionSide.Long
            ? entryPrice - initialStopLoss
            : entryPrice + initialStopLoss;

        var tp = await _binanceOrders.PlaceTakeProfitMarketAlgoOrderAsync(
            symbol,
            ToCloseSide(side),
            ToPositionSide(side),
            quantity,
            tpPrice,
            tpClientId,
            cancellationToken);

        var stop3 = await _binanceOrders.PlaceStopMarketAlgoOrderAsync(
            symbol,
            ToCloseSide(side),
            ToPositionSide(side),
            quantity,
            stop3Price,
            stop3ClientId,
            cancellationToken);

        return new BotPosition
        {
            ShortId = shortId,
            BotName = botName,
            Symbol = symbol,
            Side = side,
            Mode = PositionMode.Stop3,

            EntryPrice = entryPrice,
            Quantity = quantity,
            RemainingQuantity = quantity,

            ParentClientId = entryClientId,
            ParentOrderId = entry.OrderId,
            ParentFilledAtUtc = DateTime.UtcNow,

            TpPrice = tpPrice,
            TpClientId = tpClientId,
            TpOrderId = tp.AlgoOrderId,
            TpStatus = tp.Status,

            SlPrice = stop3Price,
            SlClientId = stop3ClientId,
            SlOrderId = stop3.AlgoOrderId,
            SlStatus = stop3.Status,

            Stop3Initial = stop3Price,
            Stop3Current = stop3Price,
            Stop3Previous = stop3Price,
            Stop3ClientId = stop3ClientId,
            Stop3OrderId = stop3.AlgoOrderId,
            Stop3Status = stop3.Status,
            Stop3Created = true,
            Stop3Pending = false,

            ProtectiveActive = true,
            Closed = false,
            Status = "OPEN",
            Source = "binance"
        };
    }

    public async Task ClosePositionAsync(
        string botName,
        BotPosition position,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(position.TpOrderId))
        {
            await _binanceOrders.CancelAlgoOrderAsync(
                position.Symbol,
                position.TpOrderId,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(position.Stop3OrderId))
        {
            await _binanceOrders.CancelAlgoOrderAsync(
                position.Symbol,
                position.Stop3OrderId,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(position.SlOrderId) &&
            position.SlOrderId != position.Stop3OrderId)
        {
            await _binanceOrders.CancelAlgoOrderAsync(
                position.Symbol,
                position.SlOrderId,
                cancellationToken);
        }

        var closeClientId = CreateClientId(botName, "CL", position.ShortId);

        var close = await _binanceOrders.PlaceMarketOrderAsync(
            position.Symbol,
            ToCloseSide(position.Side),
            ToPositionSide(position.Side),
            position.RemainingQuantity,
            closeClientId,
            cancellationToken);

        position.CloseClientId = closeClientId;
        position.CloseOrderId = close.OrderId;
        position.CloseStatus = close.Status;
        position.ProtectiveActive = false;
        position.Closed = true;
        position.ClosedAtUtc = DateTime.UtcNow;
        position.UpdatedAtUtc = DateTime.UtcNow;

        _logger.LogInformation(
            "{BotName} position closed. Id={Id}, Side={Side}",
            botName,
            position.ShortId,
            position.Side);
    }

    private static string CreateShortId()
        => Guid.NewGuid().ToString("N")[..8];

    private static string CreateClientId(string botName, string prefix, string shortId)
    {
        var shortBot = botName.Length > 8 ? botName[..8] : botName;
        var value = $"{shortBot}_{prefix}_{shortId}";

        return value[..Math.Min(32, value.Length)];
    }

    private static string ToEntrySide(PositionSide side)
        => side == PositionSide.Long ? "BUY" : "SELL";

    private static string ToCloseSide(PositionSide side)
        => side == PositionSide.Long ? "SELL" : "BUY";

    private static string ToPositionSide(PositionSide side)
        => side == PositionSide.Long ? "LONG" : "SHORT";

    private static decimal CalculatePercentPrice(
        PositionSide side,
        decimal entryPrice,
        decimal percent,
        bool isTakeProfit)
    {
        var multiplier = percent / 100m;

        if (side == PositionSide.Long)
            return isTakeProfit
                ? entryPrice * (1 + multiplier)
                : entryPrice * (1 - multiplier);

        return isTakeProfit
            ? entryPrice * (1 - multiplier)
            : entryPrice * (1 + multiplier);
    }
}