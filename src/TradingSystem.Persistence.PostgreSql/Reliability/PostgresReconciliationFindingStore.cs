using Dapper;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.Reconciliation.Contracts;
using TradingSystem.Reconciliation.Models;

namespace TradingSystem.Persistence.PostgreSql.Reliability;

public sealed class PostgresReconciliationFindingStore(
    ITradingDbConnectionFactory connections) 
    : IReconciliationFindingStore
{
    public async Task SaveRunAsync(ReconciliationRunResult result, CancellationToken ct)
    {
        await using var c = await connections.OpenAsync(ct);
        
        await using var tx = await c.BeginTransactionAsync(ct);
        
        var run = await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO trading.reconciliation_runs(started_at_utc, completed_at_utc, finding_count, healed_count) VALUES(@Started, @Completed, @Count, @Healed) RETURNING id", new{Started = result.StartedAtUtc, Completed = result.CompletedAtUtc, Count = result.Findings.Count, Healed = result.HealedCount}, tx, cancellationToken:ct));
        
        foreach(var f in result.Findings)await c.ExecuteAsync(new CommandDefinition("INSERT INTO trading.reconciliation_findings(id, run_id, detected_at_utc, bot_name, symbol, short_id, finding_type, severity, details, suggested_action, auto_heal_allowed, resolved, resolved_at_utc) VALUES(@Id, @Run, @At, @Bot, @Symbol, @Short, @Type, @Severity, @Details, @Action, @Auto, @Resolved, @ResolvedAt)", new{f.Id, Run = run, At = f.DetectedAtUtc, Bot = f.BotName, f.Symbol, Short = f.ShortId, Type = f.Type.ToString(), Severity = f.Severity.ToString(), f.Details, Action = f.SuggestedAction.ToString(), Auto = f.AutoHealAllowed, Resolved = f.AutoHealAllowed, ResolvedAt = f.AutoHealAllowed?(DateTime?)DateTime.UtcNow:null}, tx, cancellationToken:ct));await tx.CommitAsync(ct);}
    
    public async Task<bool> HasUnresolvedCriticalAsync(CancellationToken ct)
    {
        await using var c = await connections.OpenAsync(ct);
        
        return await c.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM trading.reconciliation_findings WHERE NOT resolved AND severity = 'Critical')", cancellationToken:ct));}
}
