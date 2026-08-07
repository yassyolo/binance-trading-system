namespace TradingSystem.EventStore.Models;

public sealed record EventEnvelope(
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
    string MetadataJson);
