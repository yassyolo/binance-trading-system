using TradingSystem.EventStore.Models;

namespace TradingSystem.EventStore.TradingTimeline;

public interface ITradingEventStoreReader
{
    Task<IReadOnlyList<TradingEventStoreReadItem>> ReadAsync(EventStoreQuery query, CancellationToken ct);
}
