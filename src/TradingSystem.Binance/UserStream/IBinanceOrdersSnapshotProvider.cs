using System.Text.Json;
namespace TradingSystem.Binance.UserStream;
public interface IBinanceOrdersSnapshotProvider{Task<BinanceOrdersSnapshot> GetAsync(string symbol, CancellationToken cancellationToken);}
public sealed record BinanceOrdersSnapshot(JsonElement[] NormalOrders, JsonElement[] AlgoOrders);
