using StrategyService.Configuration;
using TradingSystem.Binance.Orders;

namespace StrategyService.Services;

public sealed class SafeBinanceOrderService
{
    private readonly IBinanceFuturesOrderClient _orders;
    private readonly ILogger<SafeBinanceOrderService> _logger;

    public SafeBinanceOrderService(
        IBinanceFuturesOrderClient orders,
        ILogger<SafeBinanceOrderService> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    public async Task<bool> SafeCancelNormalAsync(
        string symbol,
        string? orderId,
        string? clientOrderId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            try
            {
                await _orders.CancelOrderAsync(symbol, orderId, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Normal cancel by order id failed. OrderId={OrderId}", orderId);
            }
        }

        return await VerifyNormalOrderAbsentAsync(
            symbol,
            orderId,
            clientOrderId,
            cancellationToken);
    }

    public async Task<bool> SafeCancelAlgoAsync(
        string symbol,
        string? algoOrderId,
        string? clientAlgoId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(algoOrderId))
        {
            for (var i = 0; i < 2; i++)
            {
                try
                {
                    await _orders.CancelAlgoOrderAsync(symbol, algoOrderId, cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Algo cancel failed. Attempt={Attempt}, AlgoId={AlgoId}, ClientAlgoId={ClientAlgoId}",
                        i + 1,
                        algoOrderId,
                        clientAlgoId);

                    await Task.Delay(150, cancellationToken);
                }
            }
        }

        return await VerifyAlgoOrderAbsentAsync(
            symbol,
            algoOrderId,
            clientAlgoId,
            cancellationToken);
    }

    public async Task<bool> VerifyNormalOrderAbsentAsync(
        string symbol,
        string? orderId,
        string? clientOrderId,
        CancellationToken cancellationToken)
    {
        var openOrders = await _orders.GetOpenOrdersAsync(symbol, cancellationToken);

        var exists = openOrders.Any(x =>
            (!string.IsNullOrWhiteSpace(orderId) && x.OrderId == orderId) ||
            (!string.IsNullOrWhiteSpace(clientOrderId) && x.ClientOrderId == clientOrderId));

        return !exists;
    }

    public async Task<bool> VerifyAlgoOrderAbsentAsync(
        string symbol,
        string? algoOrderId,
        string? clientAlgoId,
        CancellationToken cancellationToken)
    {
        var openAlgoOrders = await _orders.GetOpenAlgoOrdersAsync(symbol, cancellationToken);

        var exists = openAlgoOrders.Any(x =>
            (!string.IsNullOrWhiteSpace(algoOrderId) && x.AlgoOrderId == algoOrderId) ||
            (!string.IsNullOrWhiteSpace(clientAlgoId) && x.ClientAlgoId == clientAlgoId));

        return !exists;
    }
}