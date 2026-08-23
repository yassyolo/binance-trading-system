using Microsoft.Extensions.Logging;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Orders.Models;

namespace TradingSystem.Binance.Orders;

public sealed class SafeBinanceOrderService(
    IBinanceFuturesOrderClient orders,
    ILogger<SafeBinanceOrderService> logger)
{
    private static readonly TimeSpan FillWaitTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan RecoveryDelay = TimeSpan.FromMilliseconds(500);

    public async Task<BinanceOrderResult> SafePlaceMarketOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        string clientOrderId,
        CancellationToken ct)
    {
        try
        {
            return await orders.PlaceMarketOrderAsync(symbol, side, positionSide, quantity, clientOrderId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Market order submit outcome is unknown. Recovering by client order id. Symbol = {Symbol}, ClientOrderId = {ClientOrderId}",
                symbol,
                clientOrderId);

            return await RecoverOrderByClientIdAsync(symbol, clientOrderId, exception, ct);
        }
    }

    public async Task<BinanceOrderResult> WaitForFillAsync(
        string symbol,
        BinanceOrderResult submittedOrder,
        string clientOrderId,
        CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.Add(FillWaitTimeout);
        Exception? lastLookupException = null;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            if (IsFilled(submittedOrder))
            {
                var resolved = await ResolveFilledOrderDetailsAsync(symbol, submittedOrder, clientOrderId, ct);
                if (resolved is not null)
                    return resolved;
            }
            else
            {
                ThrowIfTerminal(submittedOrder);
            }

            try
            {
                submittedOrder = !string.IsNullOrWhiteSpace(submittedOrder.OrderId)
                    ? await orders.GetOrderAsync(symbol, submittedOrder.OrderId, ct)
                    : await orders.GetOrderByClientOrderIdAsync(symbol, clientOrderId, ct);

                lastLookupException = null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastLookupException = exception;

                logger.LogWarning(
                    exception,
                    "Order status lookup failed. Falling back to client order id. Symbol = {Symbol}, ClientOrderId = {ClientOrderId}",
                    symbol,
                    clientOrderId);

                try
                {
                    submittedOrder = await orders.GetOrderByClientOrderIdAsync(symbol, clientOrderId, ct);
                    lastLookupException = null;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception recoveryException)
                {
                    lastLookupException = recoveryException;
                }
            }

            await Task.Delay(250, ct);
        }

        try
        {
            var recovered = await orders.GetOrderByClientOrderIdAsync(symbol, clientOrderId, ct);

            if (IsFilled(recovered))
            {
                var resolved = await ResolveFilledOrderDetailsAsync(symbol, recovered, clientOrderId, ct);
                if (resolved is not null)
                    return resolved;

                throw new TimeoutException(
                    $"Binance order '{clientOrderId}' is FILLED but authoritative fill economics could not be resolved.");
            }

            ThrowIfTerminal(recovered);

            throw new TimeoutException(
                $"Order '{clientOrderId}' was found on Binance but was not filled within {FillWaitTimeout.TotalSeconds:0} seconds. Status = {recovered.Status}.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new TimeoutException(
                $"Could not resolve final Binance state for client order '{clientOrderId}'.",
                lastLookupException ?? exception);
        }
    }

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

    private async Task<BinanceOrderResult?> ResolveFilledOrderDetailsAsync(
        string symbol,
        BinanceOrderResult order,
        string clientOrderId,
        CancellationToken ct)
    {
        if (HasUsableFillEconomics(order))
            return order;

        logger.LogWarning(
            "Binance order is FILLED but fill economics are incomplete. Re-querying authoritative state. Symbol = {Symbol}, OrderId = {OrderId}, ClientOrderId = {ClientOrderId}",
            symbol,
            order.OrderId,
            clientOrderId);

        BinanceOrderResult refreshed;

        try
        {
            refreshed = !string.IsNullOrWhiteSpace(order.OrderId)
                ? await orders.GetOrderAsync(symbol, order.OrderId, ct)
                : await orders.GetOrderByClientOrderIdAsync(symbol, clientOrderId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not re-query FILLED Binance order before trade-fill recovery.");
            refreshed = order;
        }

        if (IsFilled(refreshed) && HasUsableFillEconomics(refreshed))
            return refreshed;

        if (string.IsNullOrWhiteSpace(order.OrderId))
            return null;

        try
        {
            var fills = await orders.GetTradeFillsForOrderAsync(symbol, order.OrderId, ct);
            var executedQuantity = fills.Sum(x => x.Quantity);
            var quoteQuantity = fills.Sum(x => x.QuoteQuantity > 0 ? x.QuoteQuantity : x.Price * x.Quantity);

            if (executedQuantity <= 0 || quoteQuantity <= 0)
                return null;

            var averagePrice = quoteQuantity / executedQuantity;

            logger.LogInformation(
                "Recovered authoritative Binance fill economics from user trades. Symbol = {Symbol}, OrderId = {OrderId}, ClientOrderId = {ClientOrderId}, AveragePrice = {AveragePrice}, ExecutedQuantity = {ExecutedQuantity}",
                symbol,
                order.OrderId,
                clientOrderId,
                averagePrice,
                executedQuantity);

            return refreshed with
            {
                AveragePrice = averagePrice,
                ExecutedQuantity = executedQuantity,
                CumulativeQuoteQuantity = quoteQuantity
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not recover Binance fill economics from user trades.");
            return null;
        }
    }

    private async Task<BinanceOrderResult> RecoverOrderByClientIdAsync(
        string symbol,
        string clientOrderId,
        Exception originalException,
        CancellationToken ct)
    {
        Exception? lastException = originalException;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            if (attempt > 1)
                await Task.Delay(RecoveryDelay, ct);

            try
            {
                var recovered = await orders.GetOrderByClientOrderIdAsync(symbol, clientOrderId, ct);

                logger.LogInformation(
                    "Recovered Binance order after uncertain submit outcome. Symbol = {Symbol}, ClientOrderId = {ClientOrderId}, OrderId = {OrderId}, Status = {Status}",
                    symbol,
                    clientOrderId,
                    recovered.OrderId,
                    recovered.Status);

                return recovered;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastException = exception;
                logger.LogWarning(exception, "Could not recover Binance order by client order id. Attempt = {Attempt}/3", attempt);
            }
        }

        throw new InvalidOperationException(
            $"Binance market order outcome is unknown for client order '{clientOrderId}'. Automatic resubmission is intentionally disabled to avoid duplicate exposure.",
            lastException);
    }

    private static bool IsFilled(BinanceOrderResult order)
        => order.Status?.Equals("FILLED", StringComparison.OrdinalIgnoreCase) == true;

    private static bool HasUsableFillEconomics(BinanceOrderResult order)
        => order.AveragePrice is > 0 ||
           order.ExecutedQuantity is > 0 && order.CumulativeQuoteQuantity is > 0;

    private static void ThrowIfTerminal(BinanceOrderResult order)
    {
        if (order.Status is "CANCELED" or "EXPIRED" or "REJECTED")
            throw new InvalidOperationException($"Order '{order.ClientOrderId}' is terminal: {order.Status}.");
    }
}
