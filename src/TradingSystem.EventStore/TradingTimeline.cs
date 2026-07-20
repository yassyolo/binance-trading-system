namespace TradingSystem.EventStore;

public sealed record TradingTimelineItem(
    long GlobalPosition, 
    Guid EventId, 
    DateTime OccurredAtUtc, 
    string EventType, 
    string AggregateType, 
    string AggregateId, 
    string? BotName, 
    string? Symbol, 
    string? PositionId, 
    string? SignalId, 
    string? CorrelationId, 
    string PayloadJson, 
    string MetadataJson);

public interface ITradingTimelineReader
{
    Task<IReadOnlyList<TradingTimelineItem>> ReadAsync(EventStoreQuery query,  CancellationToken cancellationToken);
}

public sealed class TradingTimelineReader(ITradingEventStore store) : ITradingTimelineReader
{
    public async Task<IReadOnlyList<TradingTimelineItem>> ReadAsync(EventStoreQuery query,  CancellationToken cancellationToken)
         =>  (await store.ReadAsync(query,  cancellationToken))
            .Select(x  =>  new TradingTimelineItem(
                x.GlobalPosition,  x.Event.EventId,  x.Event.OccurredAtUtc,  x.Event.EventType, 
                x.Event.AggregateType,  x.Event.AggregateId,  x.Event.BotName,  x.Event.Symbol, 
                x.Event.PositionId,  x.Event.SignalId,  x.Event.CorrelationId, 
                x.Event.PayloadJson,  x.Event.MetadataJson))
            .ToArray();
}
