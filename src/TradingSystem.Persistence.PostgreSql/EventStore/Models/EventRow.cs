using TradingSystem.EventStore.Models;

namespace TradingSystem.Persistence.PostgreSql.EventStore.Models;

public sealed record EventRow(
    long GlobalPosition,
    Guid EventId,
    string EventType,
    int EventVersion,
    string AggregateType,
    string AggregateId,
    long AggregateVersion,
    DateTime OccurredAtUtc,
    DateTime RecordedAtUtc,
    string? BotName,
    string? Symbol,
    string? PositionId,
    string? SignalId,
    string? CorrelationId,
    string? CausationId,
    string? Actor,
    string PayloadJson,
    string MetadataJson)
{
    public StoredTradingEvent ToStored()
        => new(GlobalPosition,
            new EventEnvelope(
                EventId,
                EventType,
                EventVersion,
                AggregateType,
                AggregateId,
                AggregateVersion,
                OccurredAtUtc,
                RecordedAtUtc,
                BotName,
                Symbol,
                PositionId,
                SignalId,
                CorrelationId,
                CausationId,
                Actor,
                PayloadJson,
                MetadataJson));
}
