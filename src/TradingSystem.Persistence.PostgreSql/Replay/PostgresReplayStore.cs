using System.Text.Json;
using Dapper;
using TradingSystem.EventStore;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.ReplayEngine;

namespace TradingSystem.Persistence.PostgreSql.Replay;

public sealed class PostgresReplayStore(ITradingDbConnectionFactory connections) : IReplayJobStore,  IReplayEventSource
{
    public async Task<Guid> EnqueueAsync(CreateReplayRequest request,  string requestedBy,  CancellationToken ct)
    {
        var id  =  Guid.NewGuid();
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition("""
            insert into trading_replay.jobs
                (replay_id, name, mode, status, requested_by, request, created_at_utc)
            values (@id, @name, @mode, 'Pending', @requestedBy, cast(@request as jsonb), now());
            """,  new { id,  name  =  request.Name,  mode  =  request.Mode.ToString(),  requestedBy,  request  =  JsonSerializer.Serialize(request,  EventJson.Options) },  cancellationToken: ct));
        return id;
    }

    public async Task<IReadOnlyList<ReplayJob>> ClaimAsync(string workerId,  int take,  TimeSpan staleAfter,  CancellationToken ct)
    {
        const string sql  =  """
            with picked as (
                select replay_id from trading_replay.jobs
                where cancellation_requested = false and (status = 'Pending' or (status = 'Processing' and processing_started_at_utc < now()-@staleAfter))
                order by created_at_utc for update skip locked limit @take)
            update trading_replay.jobs j
            set status = 'Processing', processing_worker_id = @workerId, processing_started_at_utc = now(), 
                started_at_utc = coalesce(started_at_utc, now()), attempt_count = attempt_count+1
            from picked where j.replay_id = picked.replay_id
            returning j.replay_id ReplayId, j.name Name, j.mode Mode, j.status Status, j.requested_by RequestedBy, j.request::text RequestJson, 
                      j.last_global_position LastGlobalPosition, j.processed_events ProcessedEvents, j.failed_events FailedEvents, 
                      j.progress_percent ProgressPercent, j.progress_stage ProgressStage, j.error Error, j.created_at_utc CreatedAtUtc, 
                      j.started_at_utc StartedAtUtc, j.completed_at_utc CompletedAtUtc, j.deterministic_hash DeterministicHash;
            """;
        await using var connection  =  await connections.OpenAsync(ct);
        var rows  =  await connection.QueryAsync<JobRow>(new CommandDefinition(sql,  new { workerId,  take  =  Math.Clamp(take,  1,  10),  staleAfter },  cancellationToken: ct));
        return rows.Select(Map).ToArray();
    }

    public async Task<ReplayJob?> GetAsync(Guid replayId,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        var row  =  await connection.QuerySingleOrDefaultAsync<JobRow>(new CommandDefinition("select replay_id ReplayId, name Name, mode Mode, status Status, requested_by RequestedBy, request::text RequestJson, last_global_position LastGlobalPosition, processed_events ProcessedEvents, failed_events FailedEvents, progress_percent ProgressPercent, progress_stage ProgressStage, error Error, created_at_utc CreatedAtUtc, started_at_utc StartedAtUtc, completed_at_utc CompletedAtUtc, deterministic_hash DeterministicHash from trading_replay.jobs where replay_id = @replayId",  new { replayId },  cancellationToken: ct));
        return row is null ? null : Map(row);
    }

    public async Task<ReplaySummary?> GetSummaryAsync(Guid replayId,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        var json  =  await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "select summary::text from trading_replay.results where replay_id = @replayId", 
            new { replayId },  cancellationToken: ct));
        return json is null ? null : JsonSerializer.Deserialize<ReplaySummary>(json,  EventJson.Options);
    }

    public async Task<IReadOnlyList<ReplayStepResult>> GetStepsAsync(Guid replayId,  long afterGlobalPosition,  int take,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        var rows  =  await connection.QueryAsync<ReplayStepResult>(new CommandDefinition("""
            select replay_id ReplayId, global_position GlobalPosition, source_event_id SourceEventId, 
                   event_type EventType, virtual_time_utc VirtualTimeUtc, succeeded Succeeded, 
                   result::text ResultJson, error Error
            from trading_replay.steps
            where replay_id = @replayId and global_position>@afterGlobalPosition
            order by global_position asc limit @take;
            """,  new { replayId,  afterGlobalPosition  =  Math.Max(0,  afterGlobalPosition),  take  =  Math.Clamp(take,  1,  1000) },  cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ReplayJob>> QueryAsync(ReplayJobStatus? status,  int skip,  int take,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        var rows  =  await connection.QueryAsync<JobRow>(new CommandDefinition("""
            select replay_id ReplayId, name Name, mode Mode, status Status, requested_by RequestedBy, request::text RequestJson, last_global_position LastGlobalPosition, processed_events ProcessedEvents, failed_events FailedEvents, progress_percent ProgressPercent, progress_stage ProgressStage, error Error, created_at_utc CreatedAtUtc, started_at_utc StartedAtUtc, completed_at_utc CompletedAtUtc, deterministic_hash DeterministicHash from trading_replay.jobs
            where (@status is null or status = @status)
            order by created_at_utc desc offset @skip limit @take
            """,  new { status  =  status?.ToString(),  skip  =  Math.Max(0,  skip),  take  =  Math.Clamp(take,  1,  500) },  cancellationToken: ct));
        return rows.Select(Map).ToArray();
    }

    public async Task<ReplayAccumulator?> LoadCheckpointAsync(Guid replayId,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        var json  =  await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "select state::text from trading_replay.checkpoints where replay_id = @replayId", 
            new { replayId },  cancellationToken: ct));
        return json is null ? null : JsonSerializer.Deserialize<ReplayAccumulator>(json,  EventJson.Options);
    }

    public async Task SaveCheckpointAsync(Guid replayId,  long globalPosition,  ReplayAccumulator accumulator,  int progressPercent,  string progressStage,  CancellationToken ct)
    {
        var state  =  JsonSerializer.Serialize(accumulator,  EventJson.Options);
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition("""
            insert into trading_replay.checkpoints(replay_id, last_global_position, state, updated_at_utc)
            values(@replayId, @globalPosition, cast(@state as jsonb), now())
            on conflict(replay_id) do update set last_global_position = excluded.last_global_position, state = excluded.state, updated_at_utc = now();
            update trading_replay.jobs set last_global_position = @globalPosition, processed_events = @processed, 
                failed_events = @failed, progress_percent = @progressPercent, progress_stage = @progressStage where replay_id = @replayId;
            """,  new { replayId,  globalPosition,  state,  processed  =  accumulator.ProcessedEvents,  failed  =  accumulator.FailedEvents,  progressPercent  =  Math.Clamp(progressPercent,  0,  99),  progressStage },  cancellationToken: ct));
    }

    public async Task SaveStepsAsync(IReadOnlyCollection<ReplayStepResult> steps,  CancellationToken ct)
    {
        if (steps.Count == 0) return;
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition("""
            insert into trading_replay.steps
                (replay_id, global_position, source_event_id, event_type, virtual_time_utc, succeeded, result, error)
            values(@ReplayId, @GlobalPosition, @SourceEventId, @EventType, @VirtualTimeUtc, @Succeeded, cast(@ResultJson as jsonb), @Error)
            on conflict(replay_id, global_position) do nothing;
            """,  steps,  cancellationToken: ct));
    }

    public async Task CompleteAsync(Guid replayId,  ReplaySummary summary,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition("""
            insert into trading_replay.results(replay_id, summary, deterministic_hash, created_at_utc)
            values(@replayId, cast(@summary as jsonb), @hash, now())
            on conflict(replay_id) do update set summary = excluded.summary, deterministic_hash = excluded.deterministic_hash, created_at_utc = now();
            update trading_replay.jobs set status = 'Completed', progress_percent = 100, progress_stage = 'Completed', 
                processed_events = @processed, failed_events = @failed, deterministic_hash = @hash, completed_at_utc = now(), processing_worker_id = null
            where replay_id = @replayId;
            """,  new { replayId,  summary  =  JsonSerializer.Serialize(summary,  EventJson.Options),  hash  =  summary.DeterministicHash,  processed  =  summary.ProcessedEvents,  failed  =  summary.FailedEvents },  cancellationToken: ct));
    }

    public async Task FailAsync(Guid replayId,  string error,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition("update trading_replay.jobs set status = 'Failed', error = @error, completed_at_utc = now(), processing_worker_id = null where replay_id = @replayId",  new { replayId,  error  =  error[..Math.Min(error.Length,  4000)] },  cancellationToken: ct));
    }

    public async Task CancelAsync(Guid replayId,  string actor,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition("update trading_replay.jobs set cancellation_requested = true, cancelled_by = @actor, status = case when status = 'Pending' then 'Cancelled' else status end, completed_at_utc = case when status = 'Pending' then now() else completed_at_utc end where replay_id = @replayId and status in ('Pending', 'Processing', 'Paused')",  new { replayId,  actor },  cancellationToken: ct));
    }

    public async Task MarkCancelledAsync(Guid replayId,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            "update trading_replay.jobs set status = 'Cancelled', completed_at_utc = now(), processing_worker_id = null where replay_id = @replayId", 
            new { replayId },  cancellationToken: ct));
    }

    public async Task<bool> IsCancellationRequestedAsync(Guid replayId,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("select cancellation_requested from trading_replay.jobs where replay_id = @replayId",  new { replayId },  cancellationToken: ct));
    }

    public async Task<IReadOnlyList<StoredTradingEvent>> ReadForwardAsync(CreateReplayRequest request,  long afterGlobalPosition,  int take,  CancellationToken ct)
    {
        const string sql  =  """
            select global_position GlobalPosition, event_id EventId, event_type EventType, event_version EventVersion, 
                   aggregate_type AggregateType, aggregate_id AggregateId, aggregate_version AggregateVersion, 
                   occurred_at_utc OccurredAtUtc, recorded_at_utc RecordedAtUtc, bot_name BotName, symbol Symbol, 
                   position_id PositionId, signal_id SignalId, correlation_id CorrelationId, causation_id CausationId, 
                   actor Actor, payload::text PayloadJson, metadata::text MetadataJson
            from trading_event_store.events
            where global_position>@after
              and (@fromPosition is null or global_position>=@fromPosition)
              and (@toPosition is null or global_position<=@toPosition)
              and (@fromUtc is null or occurred_at_utc>=@fromUtc)
              and (@toUtc is null or occurred_at_utc<=@toUtc)
              and (@botName is null or bot_name = @botName)
              and (@symbol is null or symbol = @symbol)
              and (@correlationId is null or correlation_id = @correlationId)
            order by global_position asc limit @take;
            """;
        await using var connection  =  await connections.OpenAsync(ct);
        var rows  =  await connection.QueryAsync<EventRow>(new CommandDefinition(sql,  new { after  =  afterGlobalPosition,  fromPosition  =  request.FromGlobalPosition,  toPosition  =  request.ToGlobalPosition,  request.FromUtc,  request.ToUtc,  request.BotName,  request.Symbol,  request.CorrelationId,  take  =  Math.Clamp(take,  1,  1000) },  cancellationToken: ct));
        return rows.Select(x  =>  x.ToStored()).ToArray();
    }

    public async Task<long> CountAsync(CreateReplayRequest request,  CancellationToken ct)
    {
        await using var connection  =  await connections.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition("""
            select count(*) from trading_event_store.events
            where (@fromPosition is null or global_position>=@fromPosition)
              and (@toPosition is null or global_position<=@toPosition)
              and (@fromUtc is null or occurred_at_utc>=@fromUtc)
              and (@toUtc is null or occurred_at_utc<=@toUtc)
              and (@botName is null or bot_name = @botName)
              and (@symbol is null or symbol = @symbol)
              and (@correlationId is null or correlation_id = @correlationId);
            """,  new { fromPosition  =  request.FromGlobalPosition,  toPosition  =  request.ToGlobalPosition,  request.FromUtc,  request.ToUtc,  request.BotName,  request.Symbol,  request.CorrelationId },  cancellationToken: ct));
    }

    private static ReplayJob Map(JobRow row)  =>  new(row.ReplayId,  row.Name,  Enum.Parse<ReplayMode>(row.Mode), 
        Enum.Parse<ReplayJobStatus>(row.Status),  row.RequestedBy, 
        JsonSerializer.Deserialize<CreateReplayRequest>(row.RequestJson,  EventJson.Options)!, 
        row.LastGlobalPosition,  row.ProcessedEvents,  row.FailedEvents,  row.ProgressPercent, 
        row.ProgressStage,  row.Error,  row.CreatedAtUtc,  row.StartedAtUtc,  row.CompletedAtUtc, 
        row.DeterministicHash);

    private sealed record JobRow(Guid ReplayId,  string Name,  string Mode,  string Status,  string RequestedBy, 
        string RequestJson,  long LastGlobalPosition,  long ProcessedEvents,  long FailedEvents, 
        int ProgressPercent,  string? ProgressStage,  string? Error,  DateTime CreatedAtUtc, 
        DateTime? StartedAtUtc,  DateTime? CompletedAtUtc,  string? DeterministicHash);
    private sealed record EventRow(long GlobalPosition, Guid EventId, string EventType, int EventVersion, string AggregateType, string AggregateId, long AggregateVersion, DateTime OccurredAtUtc, DateTime RecordedAtUtc, string? BotName, string? Symbol, string? PositionId, string? SignalId, string? CorrelationId, string? CausationId, string? Actor, string PayloadJson, string MetadataJson)
    { public StoredTradingEvent ToStored() => new(GlobalPosition, new EventEnvelope(EventId, EventType, EventVersion, AggregateType, AggregateId, AggregateVersion, OccurredAtUtc, RecordedAtUtc, BotName, Symbol, PositionId, SignalId, CorrelationId, CausationId, Actor, PayloadJson, MetadataJson)); }
}
