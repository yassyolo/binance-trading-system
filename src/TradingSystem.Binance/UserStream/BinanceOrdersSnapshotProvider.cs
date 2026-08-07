using System.Text.Json;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.UserStream.Contracts;
using TradingSystem.Binance.UserStream.Models;

namespace TradingSystem.Binance.UserStream;

public sealed class BinanceOrdersSnapshotProvider(
    IBinanceFuturesOrderClient orders)
    : IBinanceOrdersSnapshotProvider
{
    public async Task<BinanceOrdersSnapshot> GetAsync(
        string symbol,
        CancellationToken ct)
    {
        var normalOrdersTask = orders.GetOpenOrdersAsync(symbol, ct);
        var algoOrdersTask = orders.GetOpenAlgoOrdersAsync(symbol, ct);

        await Task.WhenAll(normalOrdersTask, algoOrdersTask);

        var normalOrders = await normalOrdersTask;
        var algoOrders = await algoOrdersTask;

        return new BinanceOrdersSnapshot(
            normalOrders.Select(o => JsonSerializer.SerializeToElement(o)).ToArray(),
            algoOrders.Select(order => JsonSerializer.SerializeToElement(order)).ToArray());
    }
}