using TradingSystem.EventStore.Models;

namespace TradingSystem.EventStore.Contracts;

public interface ITradingEventStore
{
    Task<StoredTradingEvent> AppendAsync(AppendTradingEvent request, CancellationToken ct);
    
    Task<IReadOnlyList<StoredTradingEvent>> ReadAsync(EventStoreQuery query, CancellationToken ct);
    
    Task<IReadOnlyList<StoredTradingEvent>> ReadStreamAsync(
        string aggregateType, 
        string aggregateId, 
        long afterVersion, 
        int take,
        CancellationToken ct);
}
