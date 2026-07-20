using Microsoft.Extensions.Logging;
using TradingSystem.Binance.Orders.Contracts;

namespace TradingSystem.Binance.Orders;

public sealed class SafeBinanceOrderService(IBinanceFuturesOrderClient orders,  ILogger<SafeBinanceOrderService> logger)
{
    public async Task<bool> SafeCancelNormalAsync(string symbol,  string? orderId,  string? clientOrderId,  CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            try { await orders.CancelOrderAsync(symbol,  orderId,  ct); return true; }
            catch (Exception ex) { logger.LogWarning(ex,  "Normal cancel failed. OrderId = {OrderId}",  orderId); }
        }
        return await VerifyNormalOrderAbsentAsync(symbol,  orderId,  clientOrderId,  ct);
    }

    public async Task<bool> SafeCancelAlgoAsync(string symbol,  string? algoOrderId,  string? clientAlgoId,  CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(algoOrderId))
        {
            try { await orders.CancelAlgoOrderAsync(symbol,  algoOrderId,  ct); return true; }
            catch (Exception ex) { logger.LogWarning(ex,  "Algo cancel failed. AlgoId = {AlgoId},  ClientAlgoId = {ClientAlgoId}",  algoOrderId,  clientAlgoId); }
        }
        return await VerifyAlgoOrderAbsentAsync(symbol,  algoOrderId,  clientAlgoId,  ct);
    }

    public async Task<bool> VerifyNormalOrderAbsentAsync(string symbol,  string? orderId,  string? clientOrderId,  CancellationToken ct)
    {
        var open  =  await orders.GetOpenOrdersAsync(symbol,  ct);
        return !open.Any(x  =>  (!string.IsNullOrWhiteSpace(orderId)  &&  x.OrderId == orderId)  ||  (!string.IsNullOrWhiteSpace(clientOrderId)  &&  x.ClientOrderId == clientOrderId));
    }

    public async Task<bool> VerifyAlgoOrderAbsentAsync(string symbol,  string? algoOrderId,  string? clientAlgoId,  CancellationToken ct)
    {
        var open  =  await orders.GetOpenAlgoOrdersAsync(symbol,  ct);
        return !open.Any(x  =>  (!string.IsNullOrWhiteSpace(algoOrderId)  &&  x.AlgoOrderId == algoOrderId)  ||  (!string.IsNullOrWhiteSpace(clientAlgoId)  &&  x.ClientAlgoId == clientAlgoId));
    }
}
