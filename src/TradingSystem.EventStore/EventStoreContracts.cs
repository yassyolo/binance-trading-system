using System.Text.Json;

namespace TradingSystem.EventStore;

public static class TradingEventTypes
{
    public const string SignalReceived  =  "trading.signal.received";
    public const string SignalRejected  =  "trading.signal.rejected";
    public const string StrategyDecisionTaken  =  "trading.strategy.decision-taken";
    public const string RiskDecisionTaken  =  "trading.risk.decision-taken";
    public const string ExecutionRequested  =  "trading.execution.requested";
    public const string ExecutionCompleted  =  "trading.execution.completed";
    public const string ExecutionFailed  =  "trading.execution.failed";
    public const string PositionOpened  =  "trading.position.opened";
    public const string PositionClosed  =  "trading.position.closed";
    public const string BotRuntimeChanged  =  "bot.runtime.changed";
    public const string BotConfigurationChanged  =  "bot.configuration.changed";
    public const string AlertRaised  =  "operations.alert.raised";
    public const string AlertResolved  =  "operations.alert.resolved";
    public const string JobStatusChanged  =  "jobs.status.changed";
}

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

public sealed record AppendTradingEvent(
    string EventType, 
    string AggregateType, 
    string AggregateId, 
    object Payload, 
    int EventVersion  =  1, 
    DateTime? OccurredAtUtc  =  null, 
    string? BotName  =  null, 
    string? Symbol  =  null, 
    string? PositionId  =  null, 
    string? SignalId  =  null, 
    string? CorrelationId  =  null, 
    string? CausationId  =  null, 
    string? Actor  =  null, 
    IReadOnlyDictionary<string,  object?>? Metadata  =  null, 
    Guid? EventId  =  null);

public sealed record EventStoreQuery(
    string? AggregateType  =  null, 
    string? AggregateId  =  null, 
    string? BotName  =  null, 
    string? Symbol  =  null, 
    string? PositionId  =  null, 
    string? SignalId  =  null, 
    string? CorrelationId  =  null, 
    string? EventType  =  null, 
    DateTime? FromUtc  =  null, 
    DateTime? ToUtc  =  null, 
    long? AfterGlobalPosition  =  null, 
    int Skip  =  0, 
    int Take  =  100);

public sealed record StoredTradingEvent(long GlobalPosition,  EventEnvelope Event);

public interface ITradingEventStore
{
    Task<StoredTradingEvent> AppendAsync(AppendTradingEvent request,  CancellationToken ct);
    Task<IReadOnlyList<StoredTradingEvent>> ReadAsync(EventStoreQuery query,  CancellationToken ct);
    Task<IReadOnlyList<StoredTradingEvent>> ReadStreamAsync(string aggregateType,  string aggregateId,  long afterVersion,  int take,  CancellationToken ct);
}

public static class EventJson
{
    public static readonly JsonSerializerOptions Options  =  new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy  =  JsonNamingPolicy.CamelCase, 
        WriteIndented  =  false
    };
}
