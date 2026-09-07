using Dapper;
using TradingSystem.Operations.Contracts;
using TradingSystem.Operations.Models;
using TradingSystem.Operations.Models.Enums;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.Operations;

public sealed class OperationalAlertCandidateSource(
    ITradingDbConnectionFactory factory)
    : IAlertCandidateSource
{
    public string SourcePrefix => "operational:";

    public async Task<IReadOnlyCollection<AlertCandidate>> LoadAsync(CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
       
        var result = new List<AlertCandidate>();

        var stale = await connection.QueryAsync<(string Component, string InstanceId, DateTime LastSeenUtc)>(
            new CommandDefinition(
            """
                with latest as (
                    select 
                        component,
                        instance_id,
                        last_seen_at_utc,
                        stale_after_seconds,
                        row_number() over(partition by component order by last_seen_at_utc desc, instance_id desc) as rn
                    from trading_dashboard.service_heartbeats
                )
                select 
                    component Component,
                    instance_id InstanceId,
                    last_seen_at_utc LastSeenUtc
                from latest
                where rn = 1
                  and last_seen_at_utc + make_interval(secs => stale_after_seconds) < now();
                """,
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));

        result.AddRange(stale.Select(x => new AlertCandidate(
            $"operational:heartbeat:{x.Component}",
            AlertSeverity.Critical,
            "ServiceHeartbeatMissing",
            $"{x.Component} has no healthy heartbeat. Latest instance {x.InstanceId} is stale.",
            Metadata: new Dictionary<string, string>
            {
                ["instanceId"] = x.InstanceId,
                ["lastSeenUtc"] = x.LastSeenUtc.ToString("O")
            })));

        var findings = await connection.QueryAsync<(Guid Id, string? BotName, string? PositionId, string FindingType, string Details)>(
            new CommandDefinition(
            """
                select 
                    id Id,
                    bot_name BotName,
                    short_id PositionId,
                    finding_type FindingType,
                    details Details
                from trading.reconciliation_findings
                where severity = 'Critical' and not resolved;
                """,
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));

        result.AddRange(findings.Select(x => new AlertCandidate(
            $"operational:reconciliation:{x.Id}",
            AlertSeverity.Critical,
            "CriticalReconciliationFinding",
            x.Details,
            x.BotName,
            x.PositionId)));

        var jobs = await connection.QueryAsync<(Guid JobId, string Type, string? Error)>
            (new CommandDefinition(
            """
                select 
                    job_id JobId, 
                    type Type, 
                    error Error
                from trading_dashboard.jobs
                where status = 'Failed'
                  and completed_at_utc > now() - interval '7 day';
                """,
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));

        result.AddRange(jobs.Select(x => new AlertCandidate(
            $"operational:job:{x.JobId}",
            AlertSeverity.Warning,
            "JobFailed",
            $"{x.Type} job failed: {x.Error ?? "Unknown error"}")));

        return result;
    }
}

