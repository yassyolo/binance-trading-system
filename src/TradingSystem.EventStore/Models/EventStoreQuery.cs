namespace TradingSystem.EventStore.Models;

public sealed record EventStoreQuery(
    string? AggregateType = null,
    string? AggregateId = null,
    string? BotName = null,
    string? Symbol = null,
    string? PositionId = null,
    string? SignalId = null,
    string? CorrelationId = null,
    string? EventType = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    long? AfterGlobalPosition = null,
    int Skip = 0,
    int Take = 100);