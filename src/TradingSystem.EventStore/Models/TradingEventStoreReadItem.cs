namespace TradingSystem.EventStore.Models;

public sealed record TradingEventStoreReadItem(
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
