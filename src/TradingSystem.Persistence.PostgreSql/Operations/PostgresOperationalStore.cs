using System.Text.Json;
using Dapper;
using TradingSystem.Operations;
using TradingSystem.Operations.Enums;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.Operations;

public sealed class PostgresOperationalStore(
    ITradingDbConnectionFactory factory)
    : IServiceHeartbeatStore, IAlertStore, IAuditLog
{
    public async Task UpsertAsync(ServiceHeartbeat heartbeat, CancellationToken ct)
    {
        const string sql = """
            insert into trading_dashboard.service_heartbeats
                (component, instance_id, version, environment, status, started_at_utc,
                 last_seen_at_utc, stale_after_seconds, details)
            values
                (@ServiceName, @InstanceId, @Version, @Environment, @Status, @StartedAtUtc,
                 @LastSeenAtUtc, @StaleAfterSeconds, cast(@Details as jsonb))
            on conflict(component, instance_id) do update set
                version = excluded.version,
                environment = excluded.environment,
                status = excluded.status,
                started_at_utc = excluded.started_at_utc,
                last_seen_at_utc = excluded.last_seen_at_utc,
                stale_after_seconds = excluded.stale_after_seconds,
                details = excluded.details;
            """;

        await using var connection = await factory.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                heartbeat.ServiceName,
                heartbeat.InstanceId,
                heartbeat.Version,
                heartbeat.Environment,
                Status = heartbeat.Status.ToString(),
                heartbeat.StartedAtUtc,
                heartbeat.LastSeenAtUtc,
                heartbeat.StaleAfterSeconds,
                Details = JsonSerializer.Serialize(
                    heartbeat.Details ?? new Dictionary<string, string>())
            },
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));
    }

    public async Task UpsertActiveAsync(AlertCandidate candidate, CancellationToken ct)
    {
        const string sql = """
            insert into trading_dashboard.alerts
                (deduplication_key, severity, type, message, bot_name, position_id,
                 metadata, acknowledged, resolved, last_seen_at_utc, occurrence_count)
            values
                (@Key, @Severity, @Type, @Message, @BotName, @PositionId,
                 cast(@Metadata as jsonb), false, false, now(), 1)
            on conflict(deduplication_key) do update set
                severity = excluded.severity,
                type = excluded.type,
                message = excluded.message,
                bot_name = excluded.bot_name,
                position_id = excluded.position_id,
                metadata = excluded.metadata,
                resolved = false,
                resolved_at_utc = null,
                last_seen_at_utc = now(),
                occurrence_count = trading_dashboard.alerts.occurrence_count + 1;
            """;

        await using var connection = await factory.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Key = candidate.DeduplicationKey,
                Severity = candidate.Severity.ToString(),
                candidate.Type,
                candidate.Message,
                candidate.BotName,
                candidate.PositionId,
                Metadata = JsonSerializer.Serialize(
                    candidate.Metadata ?? new Dictionary<string, string>())
            },
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));
    }

    public async Task ResolveMissingAsync(
        string prefix,
        IReadOnlyCollection<string> active,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentNullException.ThrowIfNull(active);

        const string sql = """
            update trading_dashboard.alerts
            set resolved = true, resolved_at_utc = now()
            where not resolved
              and deduplication_key like @prefix
              and not (deduplication_key = any(@active));
            """;

        await using var connection = await factory.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                prefix = prefix + "%",
                active = active.Distinct(StringComparer.Ordinal).ToArray()
            },
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));
    }

    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        const string sql = """
            insert into trading_dashboard.audit_events
                (audit_id, occurred_at_utc, actor, action, entity_type, entity_id,
                 reason, correlation_id, ip_address, old_value, new_value, metadata)
            values
                (@AuditId, @OccurredAtUtc, @Actor, @Action, @EntityType, @EntityId,
                 @Reason, @CorrelationId, @IpAddress, cast(@OldValueJson as jsonb),
                 cast(@NewValueJson as jsonb), cast(@Metadata as jsonb));
            """;

        await using var connection = await factory.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                auditEvent.AuditId,
                auditEvent.OccurredAtUtc,
                auditEvent.Actor,
                auditEvent.Action,
                auditEvent.EntityType,
                auditEvent.EntityId,
                auditEvent.Reason,
                auditEvent.CorrelationId,
                auditEvent.IpAddress,
                OldValueJson = auditEvent.OldValueJson ?? "null",
                NewValueJson = auditEvent.NewValueJson ?? "null",
                Metadata = JsonSerializer.Serialize(
                    auditEvent.Metadata ?? new Dictionary<string, string>())
            },
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));
    }
}

public sealed class OperationalAlertCandidateSource(
    ITradingDbConnectionFactory factory) : IAlertCandidateSource
{
    public string SourcePrefix => "operational:";

    public async Task<IReadOnlyCollection<AlertCandidate>> LoadAsync(
        CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        var result = new List<AlertCandidate>();

        var stale = await connection.QueryAsync<(
            string Component,
            string InstanceId,
            DateTime LastSeenUtc)>(new CommandDefinition(
            """
                with latest as (
                    select component,
                           instance_id,
                           last_seen_at_utc,
                           stale_after_seconds,
                           row_number() over(
                               partition by component
                               order by last_seen_at_utc desc, instance_id desc) as rn
                    from trading_dashboard.service_heartbeats
                )
                select component Component,
                       instance_id InstanceId,
                       last_seen_at_utc LastSeenUtc
                from latest
                where rn = 1
                  and last_seen_at_utc + make_interval(secs => stale_after_seconds) < now();
                """,
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));

        result.AddRange(stale.Select(item => new AlertCandidate(
            $"operational:heartbeat:{item.Component}",
            AlertSeverity.Critical,
            "ServiceHeartbeatMissing",
            $"{item.Component} has no healthy heartbeat. Latest instance {item.InstanceId} is stale.",
            Metadata: new Dictionary<string, string>
            {
                ["instanceId"] = item.InstanceId,
                ["lastSeenUtc"] = item.LastSeenUtc.ToString("O")
            })));

        var findings = await connection.QueryAsync<(
            Guid Id,
            string? BotName,
            string? PositionId,
            string FindingType,
            string Details)>(new CommandDefinition(
            """
                select id Id,
                       bot_name BotName,
                       short_id PositionId,
                       finding_type FindingType,
                       details Details
                from trading.reconciliation_findings
                where severity = 'Critical' and not resolved;
                """,
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));

        result.AddRange(findings.Select(item => new AlertCandidate(
            $"operational:reconciliation:{item.Id}",
            AlertSeverity.Critical,
            "CriticalReconciliationFinding",
            item.Details,
            item.BotName,
            item.PositionId)));

        var jobs = await connection.QueryAsync<(
            Guid JobId,
            string Type,
            string? Error)>(new CommandDefinition(
            """
                select job_id JobId, type Type, error Error
                from trading_dashboard.jobs
                where status = 'Failed'
                  and completed_at_utc > now() - interval '7 day';
                """,
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));

        result.AddRange(jobs.Select(item => new AlertCandidate(
            $"operational:job:{item.JobId}",
            AlertSeverity.Warning,
            "JobFailed",
            $"{item.Type} job failed: {item.Error ?? "Unknown error"}")));

        return result;
    }
}
