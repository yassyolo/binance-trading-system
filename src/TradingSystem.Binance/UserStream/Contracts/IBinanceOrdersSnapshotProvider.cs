using System.Text.Json;
using TradingSystem.Binance.UserStream.Models;

namespace TradingSystem.Binance.UserStream.Contracts;

public interface IBinanceOrdersSnapshotProvider
{
    Task<BinanceOrdersSnapshot> GetAsync(string symbol, CancellationToken ct);
}