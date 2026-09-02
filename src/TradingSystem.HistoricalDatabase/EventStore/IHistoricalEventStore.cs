using TradingSystem.HistoricalDatabase.Models;

namespace TradingSystem.HistoricalDatabase.EventStore;

public interface IHistoricalEventStore
{
    Task AppendAsync(HistoricalEvent historicalEvent, CancellationToken ct);

    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct);
}