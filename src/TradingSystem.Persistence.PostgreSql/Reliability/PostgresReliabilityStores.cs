using System.Text.Json;
using Dapper;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.Reconciliation;
using TradingSystem.RiskManagement;

namespace TradingSystem.Persistence.PostgreSql.Reliability;

public sealed class PostgresReconciliationFindingStore(ITradingDbConnectionFactory connections) : IReconciliationFindingStore
{
 public async Task SaveRunAsync(ReconciliationRunResult result, CancellationToken ct){await using var c = await connections.OpenAsync(ct);await using var tx = await c.BeginTransactionAsync(ct);var run = await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO trading.reconciliation_runs(started_at_utc, completed_at_utc, finding_count, healed_count) VALUES(@Started, @Completed, @Count, @Healed) RETURNING id", new{Started = result.StartedAtUtc, Completed = result.CompletedAtUtc, Count = result.Findings.Count, Healed = result.HealedCount}, tx, cancellationToken:ct));foreach(var f in result.Findings)await c.ExecuteAsync(new CommandDefinition("INSERT INTO trading.reconciliation_findings(id, run_id, detected_at_utc, bot_name, symbol, short_id, finding_type, severity, details, suggested_action, auto_heal_allowed, resolved, resolved_at_utc) VALUES(@Id, @Run, @At, @Bot, @Symbol, @Short, @Type, @Severity, @Details, @Action, @Auto, @Resolved, @ResolvedAt)", new{f.Id, Run = run, At = f.DetectedAtUtc, Bot = f.BotName, f.Symbol, Short = f.ShortId, Type = f.Type.ToString(), Severity = f.Severity.ToString(), f.Details, Action = f.SuggestedAction.ToString(), Auto = f.AutoHealAllowed, Resolved = f.AutoHealAllowed, ResolvedAt = f.AutoHealAllowed?(DateTime?)DateTime.UtcNow:null}, tx, cancellationToken:ct));await tx.CommitAsync(ct);}
 public async Task<bool> HasUnresolvedCriticalAsync(CancellationToken ct){await using var c = await connections.OpenAsync(ct);return await c.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM trading.reconciliation_findings WHERE NOT resolved AND severity = 'Critical')", cancellationToken:ct));}
}

public sealed class PostgresRiskStateProvider(ITradingDbConnectionFactory connections, IReconciliationFindingStore findings) : IRiskStateProvider
{
 public async Task<RiskStateSnapshot> GetAsync(DateTime at, CancellationToken ct){await using var c = await connections.OpenAsync(ct);var row = await c.QuerySingleAsync<(decimal pnl, decimal peak, decimal equity, int losses)>(new CommandDefinition(@"SELECT COALESCE(SUM(net_pnl), 0) pnl,  COALESCE(MAX(final_balance), 0) peak,  COALESCE((ARRAY_AGG(final_balance ORDER BY completed_at_utc DESC))[1], 0) equity,  0 losses FROM trading.performance_snapshots s JOIN trading.performance_runs r ON r.run_id = s.run_id WHERE r.run_type = 'Live' AND r.completed_at_utc>=date_trunc('day', @At)", new{At = at}, cancellationToken:ct));return new(row.pnl, row.peak, row.equity, row.losses, await findings.HasUnresolvedCriticalAsync(ct));}
}
