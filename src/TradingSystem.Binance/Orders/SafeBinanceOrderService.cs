using Microsoft.Extensions.Logging;
using TradingSystem.Binance.Orders.Contracts;

namespace TradingSystem.Binance.Orders;

public sealed class SafeBinanceOrderService(
    IBinanceFuturesOrderClient orders,
    ILogger<SafeBinanceOrderService> logger)
{
    public async Task<bool> SafeCancelNormalAsync(string symbol, string? orderId, string? clientOrderId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            try
            {
                await orders.CancelOrderAsync(symbol, orderId, ct);
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Normal cancel failed. OrderId = {OrderId}", orderId);
            }
        }

        return await VerifyNormalOrderAbsentAsync(symbol, orderId, clientOrderId, ct);
    }

    public async Task<bool> SafeCancelAlgoAsync(string symbol, string? algoOrderId, string? clientAlgoId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(algoOrderId))
        {
            try
            {
                await orders.CancelAlgoOrderAsync(symbol, algoOrderId, ct);
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Algo cancel failed. AlgoId = {AlgoId}, ClientAlgoId = {ClientAlgoId}", algoOrderId, clientAlgoId);
            }
        }

        return await VerifyAlgoOrderAbsentAsync(symbol, algoOrderId, clientAlgoId, ct);
    }

    public async Task<bool> VerifyNormalOrderAbsentAsync(string symbol, string? orderId, string? clientOrderId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(clientOrderId))
            return false;

        var openOrders = await orders.GetOpenOrdersAsync(symbol, ct);
        return !openOrders.Any(o =>
            (!string.IsNullOrWhiteSpace(orderId) && o.OrderId == orderId) ||
            (!string.IsNullOrWhiteSpace(clientOrderId) && o.ClientOrderId == clientOrderId));
    }

    public async Task<bool> VerifyAlgoOrderAbsentAsync(string symbol, string? algoOrderId, string? clientAlgoId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(algoOrderId) && string.IsNullOrWhiteSpace(clientAlgoId))
            return false;

        var openOrders = await orders.GetOpenAlgoOrdersAsync(symbol, ct);
        return !openOrders.Any(o =>
            (!string.IsNullOrWhiteSpace(algoOrderId) && o.AlgoOrderId == algoOrderId) ||
            (!string.IsNullOrWhiteSpace(clientAlgoId) && o.ClientAlgoId == clientAlgoId));
    }
}
