using System.Text.Json;
using Dapper;
using TradingSystem.EventStore;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.EventStore;

public sealed class PostgresTradingEventStore(ITradingDbConnectionFactory connections) : ITradingEventStore
{
    public async Task<StoredTradingEvent> AppendAsync(AppendTradingEvent request,  CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AggregateId);

        await using var connection  =  await connections.OpenAsync(cancellationToken);
        await using var transaction  =  await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "SELECT pg_advisory_xact_lock(hashtextextended(@StreamKey,  0));", 
            new { StreamKey  =  $"{request.AggregateType}:{request.AggregateId}" },  transaction,  cancellationToken: cancellationToken));

        const string sql  =  """
            WITH next_version AS (
                SELECT COALESCE(MAX(aggregate_version),  0) + 1 AS value
                FROM trading_event_store.events
                WHERE aggregate_type  =  @AggregateType AND aggregate_id  =  @AggregateId
            )
            INSERT INTO trading_event_store.events
                (event_id,  event_type,  event_version,  aggregate_type,  aggregate_id,  aggregate_version, 
                 occurred_at_utc,  bot_name,  symbol,  position_id,  signal_id,  correlation_id,  causation_id, 
                 actor,  payload,  metadata)
            SELECT @EventId,  @EventType,  @EventVersion,  @AggregateType,  @AggregateId,  next_version.value, 
                   @OccurredAtUtc,  @BotName,  @Symbol,  @PositionId,  @SignalId,  @CorrelationId,  @CausationId, 
                   @Actor,  CAST(@Payload AS jsonb),  CAST(@Metadata AS jsonb)
            FROM next_version
            RETURNING global_position AS GlobalPosition,  event_id AS EventId,  event_type AS EventType, 
                      event_version AS EventVersion,  aggregate_type AS AggregateType,  aggregate_id AS AggregateId, 
                      aggregate_version AS AggregateVersion,  occurred_at_utc AS OccurredAtUtc, 
                      recorded_at_utc AS RecordedAtUtc,  bot_name AS BotName,  symbol AS Symbol, 
                      position_id AS PositionId,  signal_id AS SignalId,  correlation_id AS CorrelationId, 
                      causation_id AS CausationId,  actor AS Actor,  payload::text AS PayloadJson, 
                      metadata::text AS MetadataJson;
            """;
        var row  =  await connection.QuerySingleAsync<EventRow>(new CommandDefinition(sql,  new
        {
            EventId  =  request.EventId ?? Guid.NewGuid(),  request.EventType,  request.EventVersion, 
            request.AggregateType,  request.AggregateId, 
            OccurredAtUtc  =  request.OccurredAtUtc ?? DateTime.UtcNow, 
            request.BotName,  request.Symbol,  request.PositionId,  request.SignalId, 
            request.CorrelationId,  request.CausationId,  request.Actor, 
            Payload  =  JsonSerializer.Serialize(request.Payload,  EventJson.Options), 
            Metadata  =  JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string,  object?>(),  EventJson.Options)
        },  transaction,  cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return row.ToStored();
    }

    public async Task<IReadOnlyList<StoredTradingEvent>> ReadAsync(EventStoreQuery query,  CancellationToken cancellationToken)
    {
        var take  =  Math.Clamp(query.Take,  1,  500);
        var skip  =  Math.Max(0,  query.Skip);
        const string sql  =  """
            SELECT global_position AS GlobalPosition,  event_id AS EventId,  event_type AS EventType, 
                   event_version AS EventVersion,  aggregate_type AS AggregateType,  aggregate_id AS AggregateId, 
                   aggregate_version AS AggregateVersion,  occurred_at_utc AS OccurredAtUtc, 
                   recorded_at_utc AS RecordedAtUtc,  bot_name AS BotName,  symbol AS Symbol, 
                   position_id AS PositionId,  signal_id AS SignalId,  correlation_id AS CorrelationId, 
                   causation_id AS CausationId,  actor AS Actor,  payload::text AS PayloadJson, 
                   metadata::text AS MetadataJson
            FROM trading_event_store.events
            WHERE (@AggregateType IS NULL OR aggregate_type  =  @AggregateType)
              AND (@AggregateId IS NULL OR aggregate_id  =  @AggregateId)
              AND (@BotName IS NULL OR bot_name  =  @BotName)
              AND (@Symbol IS NULL OR symbol  =  @Symbol)
              AND (@PositionId IS NULL OR position_id  =  @PositionId)
              AND (@SignalId IS NULL OR signal_id  =  @SignalId)
              AND (@CorrelationId IS NULL OR correlation_id  =  @CorrelationId)
              AND (@EventType IS NULL OR event_type  =  @EventType)
              AND (@FromUtc IS NULL OR occurred_at_utc >= @FromUtc)
              AND (@ToUtc IS NULL OR occurred_at_utc <= @ToUtc)
              AND (@AfterGlobalPosition IS NULL OR global_position > @AfterGlobalPosition)
            ORDER BY global_position DESC
            OFFSET @Skip LIMIT @Take;
            """;
        await using var connection  =  await connections.OpenAsync(cancellationToken);
        var rows  =  await connection.QueryAsync<EventRow>(new CommandDefinition(sql,  new
        {
            query.AggregateType,  query.AggregateId,  query.BotName,  query.Symbol,  query.PositionId, 
            query.SignalId,  query.CorrelationId,  query.EventType,  query.FromUtc,  query.ToUtc, 
            query.AfterGlobalPosition,  Skip  =  skip,  Take  =  take
        },  cancellationToken: cancellationToken));
        return rows.Select(x  =>  x.ToStored()).ToArray();
    }

    public async Task<IReadOnlyList<StoredTradingEvent>> ReadStreamAsync(string aggregateType,  string aggregateId,  long afterVersion,  int take,  CancellationToken cancellationToken)
    {
        const string sql  =  """
            SELECT global_position AS GlobalPosition,  event_id AS EventId,  event_type AS EventType, 
                   event_version AS EventVersion,  aggregate_type AS AggregateType,  aggregate_id AS AggregateId, 
                   aggregate_version AS AggregateVersion,  occurred_at_utc AS OccurredAtUtc, 
                   recorded_at_utc AS RecordedAtUtc,  bot_name AS BotName,  symbol AS Symbol, 
                   position_id AS PositionId,  signal_id AS SignalId,  correlation_id AS CorrelationId, 
                   causation_id AS CausationId,  actor AS Actor,  payload::text AS PayloadJson, 
                   metadata::text AS MetadataJson
            FROM trading_event_store.events
            WHERE aggregate_type  =  @AggregateType AND aggregate_id  =  @AggregateId AND aggregate_version > @AfterVersion
            ORDER BY aggregate_version ASC LIMIT @Take;
            """;
        await using var connection  =  await connections.OpenAsync(cancellationToken);
        var rows  =  await connection.QueryAsync<EventRow>(new CommandDefinition(sql,  new
        { AggregateType  =  aggregateType,  AggregateId  =  aggregateId,  AfterVersion  =  Math.Max(0,  afterVersion),  Take  =  Math.Clamp(take,  1,  1000) },  cancellationToken: cancellationToken));
        return rows.Select(x  =>  x.ToStored()).ToArray();
    }

    private sealed record EventRow(long GlobalPosition,  Guid EventId,  string EventType,  int EventVersion, 
        string AggregateType,  string AggregateId,  long AggregateVersion,  DateTime OccurredAtUtc, 
        DateTime RecordedAtUtc,  string? BotName,  string? Symbol,  string? PositionId,  string? SignalId, 
        string? CorrelationId,  string? CausationId,  string? Actor,  string PayloadJson,  string MetadataJson)
    {
        public StoredTradingEvent ToStored()  =>  new(GlobalPosition,  new EventEnvelope(EventId,  EventType, 
            EventVersion,  AggregateType,  AggregateId,  AggregateVersion,  OccurredAtUtc,  RecordedAtUtc, 
            BotName,  Symbol,  PositionId,  SignalId,  CorrelationId,  CausationId,  Actor,  PayloadJson,  MetadataJson));
    }
}
