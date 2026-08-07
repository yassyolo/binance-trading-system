namespace TradingSystem.EventStore.Models;

public sealed record AppendTradingEvent(
    string EventType,
    string AggregateType,
    string AggregateId,
    object Payload,
    int EventVersion = 1,
    DateTime? OccurredAtUtc = null,
    string? BotName = null,
    string? Symbol = null,
    string? PositionId = null,
    string? SignalId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    string? Actor = null,
    IReadOnlyDictionary<string, object?>? Metadata = null,
    Guid? EventId = null);
