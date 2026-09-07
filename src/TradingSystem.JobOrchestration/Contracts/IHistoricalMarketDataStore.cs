using TradingSystem.Domain.MarketData;
using TradingSystem.JobOrchestration.Models;

namespace TradingSystem.JobOrchestration.Contracts;

public interface IHistoricalMarketDataStore
{
    Task<IReadOnlyList<MarketCandle>> LoadCandlesAsync(string symbol, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    
    Task UpsertCandlesAsync(IReadOnlyCollection<MarketCandle> candles, CancellationToken ct);
    
    Task ReplaceGapsAsync(string symbol, string interval, IReadOnlyCollection<HistoricalDataGap> gaps, CancellationToken ct);
    
    Task<DateTime?> GetLatestOpenTimeAsync(string symbol, string interval, CancellationToken ct);
    
    Task<bool> HasGapsAsync(string symbol, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
