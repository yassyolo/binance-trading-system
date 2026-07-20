using System.Text.Json;
using TradingSystem.Binance.Orders.Contracts;

namespace TradingSystem.Binance.UserStream;

public sealed class BinanceOrdersSnapshotProvider(IBinanceFuturesOrderClient orders):IBinanceOrdersSnapshotProvider
{
    public async Task<BinanceOrdersSnapshot> GetAsync(string symbol, CancellationToken ct)
    {
        var normalTask = orders.GetOpenOrdersAsync(symbol, ct);var algoTask = orders.GetOpenAlgoOrdersAsync(symbol, ct);await Task.WhenAll(normalTask, algoTask);
        return new BinanceOrdersSnapshot(normalTask.Result.Select(JsonSerializer.SerializeToElement).ToArray(), algoTask.Result.Select(JsonSerializer.SerializeToElement).ToArray());
    }
}
