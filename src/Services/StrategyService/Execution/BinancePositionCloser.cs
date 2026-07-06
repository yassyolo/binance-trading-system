using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Execution;

public sealed class BinancePositionCloser(
    IBinanceFuturesOrderClient binanceOrders,
    ILogger<BinancePositionCloser> logger)
{
    public async Task CloseAsync(
        string botName,
        BotPosition position,
        CancellationToken cancellationToken)
    {
        position.Status = PositionStatus.Closing;
        position.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(position.TpOrderId) && !position.TpExecuted)
            await binanceOrders.CancelOrderAsync(position.Symbol, position.TpOrderId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(position.Stop3OrderId))
            await binanceOrders.CancelAlgoOrderAsync(position.Symbol, position.Stop3OrderId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(position.SlOrderId) &&
            position.SlOrderId != position.Stop3OrderId &&
            !position.SlExecuted)
            await binanceOrders.CancelAlgoOrderAsync(position.Symbol, position.SlOrderId, cancellationToken);

        if (position.RemainingQuantity <= 0)
        {
            position.MarkClosed("NO_REMAINING_QUANTITY");
            return;
        }

        var closeClientId = BinanceClientOrderIdFactory.Create(botName, "CL", position.ShortId);

        var close = await binanceOrders.PlaceMarketOrderAsync(
            position.Symbol,
            BinanceOrderSideMapper.ToCloseSide(position.Side),
            BinanceOrderSideMapper.ToPositionSide(position.Side),
            position.RemainingQuantity,
            closeClientId,
            cancellationToken);

        position.CloseClientId = closeClientId;
        position.CloseOrderId = close.OrderId;
        position.CloseStatus = close.Status;
        position.ProtectiveActive = false;

        logger.LogInformation(
            "{BotName} position closed. Id={Id}, Side={Side}",
            botName,
            position.ShortId,
            position.Side);
    }
}
