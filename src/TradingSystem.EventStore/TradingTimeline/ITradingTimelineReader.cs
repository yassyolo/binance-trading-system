using TradingSystem.EventStore.Models;

namespace TradingSystem.EventStore.TradingTimeline;

public interface ITradingTimelineReader
{
    Task<IReadOnlyList<TradingTimelineItem>> ReadAsync(EventStoreQuery query,  CancellationToken ct);
}
