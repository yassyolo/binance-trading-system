using System.Text.Json;
using Dapper;
using TradingSystem.Operations;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.Operations;

public sealed class PostgresOperationalStore(ITradingDbConnectionFactory factory) : IServiceHeartbeatStore, IAlertStore, IAuditLog
{
 public async Task UpsertAsync(ServiceHeartbeat h, CancellationToken ct){await using var c = await factory.OpenConnectionAsync(ct);await c.ExecuteAsync(new CommandDefinition(@"insert into trading_dashboard.service_heartbeats(component, instance_id, version, environment, status, started_at_utc, last_seen_utc, stale_after_seconds, details) values(@ServiceName, @InstanceId, @Version, @Environment, @Status, @StartedAtUtc, @LastSeenAtUtc, @StaleAfterSeconds, @Details) on conflict(component, instance_id) do update set version = excluded.version, environment = excluded.environment, status = excluded.status, last_seen_at_utc = excluded.last_seen_at_utc, stale_after_seconds = excluded.stale_after_seconds, details = excluded.details", new{h.ServiceName, h.InstanceId, h.Version, h.Environment, Status = h.Status.ToString(), h.StartedAtUtc, h.LastSeenAtUtc, h.StaleAfterSeconds, Details = JsonSerializer.Serialize(h.Details??new Dictionary<string, string>())}, cancellationToken:ct));}
 public async Task UpsertActiveAsync(AlertCandidate a, CancellationToken ct){await using var c = await factory.OpenConnectionAsync(ct);await c.ExecuteAsync(new CommandDefinition(@"insert into trading_dashboard.alerts(deduplication_key, severity, type, message, bot_name, position_id, metadata, acknowledged, resolved, last_seen_at_utc, occurrence_count) values(@Key, @Severity, @Type, @Message, @BotName, @PositionId, cast(@Metadata as jsonb), false, false, now(), 1) on conflict(deduplication_key) do update set severity = excluded.severity, type = excluded.type, message = excluded.message, bot_name = excluded.bot_name, position_id = excluded.position_id, metadata = excluded.metadata, resolved = false, resolved_at_utc = null, last_seen_at_utc = now(), occurrence_count = trading_dashboard.alerts.occurrence_count+1", new{Key = a.DeduplicationKey, Severity = a.Severity.ToString(), a.Type, a.Message, a.BotName, a.PositionId, Metadata = JsonSerializer.Serialize(a.Metadata??new Dictionary<string, string>())}, cancellationToken:ct));}
 public async Task ResolveMissingAsync(string prefix, IReadOnlyCollection<string> active, CancellationToken ct){await using var c = await factory.OpenConnectionAsync(ct);await c.ExecuteAsync(new CommandDefinition(@"update trading_dashboard.alerts set resolved = true, resolved_at_utc = now() where not resolved and deduplication_key like @prefix and not(deduplication_key = any(@active))", new{prefix = prefix+"%", active = active.ToArray()}, cancellationToken:ct));}
 public async Task WriteAsync(AuditEvent e, CancellationToken ct){await using var c = await factory.OpenConnectionAsync(ct);await c.ExecuteAsync(new CommandDefinition(@"insert into trading_dashboard.audit_events(audit_id, occurred_at_utc, actor, action, entity_type, entity_id, reason, correlation_id, ip_address, old_value, new_value, metadata) values(@AuditId, @OccurredAtUtc, @Actor, @Action, @EntityType, @EntityId, @Reason, @CorrelationId, @IpAddress, cast(@OldValueJson as jsonb), cast(@NewValueJson as jsonb), cast(@Metadata as jsonb))", new{e.AuditId, e.OccurredAtUtc, e.Actor, e.Action, e.EntityType, e.EntityId, e.Reason, e.CorrelationId, e.IpAddress, OldValueJson = e.OldValueJson??"null", NewValueJson = e.NewValueJson??"null", Metadata = JsonSerializer.Serialize(e.Metadata??new Dictionary<string, string>())}, cancellationToken:ct));}
}

public sealed class OperationalAlertCandidateSource(ITradingDbConnectionFactory factory):IAlertCandidateSource
{
 public string SourcePrefix => "operational:";
 public async Task<IReadOnlyCollection<AlertCandidate>> LoadAsync(CancellationToken ct)
 {
  await using var c = await factory.OpenConnectionAsync(ct);var result = new List<AlertCandidate>();
  var stale = await c.QueryAsync<(string Component, string InstanceId, DateTime LastSeenUtc)>(new CommandDefinition("select component, instance_id, last_seen_at_utc LastSeenUtc from trading_dashboard.service_heartbeats where last_seen_at_utc + make_interval(secs => stale_after_seconds)<now()", cancellationToken:ct));
  result.AddRange(stale.Select(x => new AlertCandidate($"operational:heartbeat:{x.Component}:{x.InstanceId}", AlertSeverity.Critical, "ServiceHeartbeatMissing", $"{x.Component} instance {x.InstanceId} has a stale heartbeat.", Metadata:new Dictionary<string, string>{{"lastSeenUtc", x.LastSeenUtc.ToString("O")}})));
  var findings = await c.QueryAsync<(long Id, string? BotName, string? PositionId, string FindingType, string Details)>(new CommandDefinition("select finding_id Id, bot_name BotName, short_id PositionId, finding_type FindingType, details from trading.reconciliation_findings where severity = 'Critical' and not resolved", cancellationToken:ct));
  result.AddRange(findings.Select(x => new AlertCandidate($"operational:reconciliation:{x.Id}", AlertSeverity.Critical, "CriticalReconciliationFinding", x.Details, x.BotName, x.PositionId)));
  var jobs = await c.QueryAsync<(Guid JobId, string Type, string? Error)>(new CommandDefinition("select job_id JobId, type Type, error Error from trading_dashboard.jobs where status = 'Failed' and completed_at_utc>now()-interval '7 day'", cancellationToken:ct));
  result.AddRange(jobs.Select(x => new AlertCandidate($"operational:job:{x.JobId}", AlertSeverity.Warning, "JobFailed", $"{x.Type} job failed: {x.Error??"Unknown error"}")));
  return result;
 }
}
