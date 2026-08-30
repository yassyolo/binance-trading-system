using Dapper;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.Persistence.PostgreSql.Reliability.Models;
using TradingSystem.Reconciliation.Contracts;
using TradingSystem.Reconciliation.Models;

namespace TradingSystem.Persistence.PostgreSql.Reliability;

public sealed class PostgresReconciliationFindingStore(
    ITradingDbConnectionFactory connections)
    : IReconciliationFindingStore
{
    public async Task SaveRunAsync(ReconciliationRunResult result, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var runId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO trading.reconciliation_runs
            (started_at_utc,
             completed_at_utc,
             finding_count,
             healed_count)
            VALUES(@Started, @Completed, @Count, @Healed)
            RETURNING id;
            """,
            new
            {
                Started = result.StartedAtUtc,
                Completed = result.CompletedAtUtc,
                Count = result.Findings.Count,
                Healed = result.HealedCount
            },
            transaction,
            cancellationToken: ct));

        var currentFingerprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in result.Findings)
        {
            var fingerprint = Fingerprint(f.BotName, f.Symbol, f.ShortId, f.Type.ToString());
            currentFingerprints.Add(fingerprint);

            var existingIds = (await connection.QueryAsync<Guid>(new CommandDefinition(
                """
                SELECT id
                FROM trading.reconciliation_findings
                WHERE NOT resolved
                  AND bot_name = @Bot
                  AND symbol = @Symbol
                  AND finding_type = @Type
                  AND COALESCE(short_id, '') = COALESCE(@Short, '')
                ORDER BY detected_at_utc ASC, id ASC;
                """,
                new
                {
                    Bot = f.BotName,
                    f.Symbol,
                    Short = f.ShortId,
                    Type = f.Type.ToString()
                },
                transaction,
                cancellationToken: ct))).AsList();

            if (existingIds.Count == 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO trading.reconciliation_findings(
                        id,
                        run_id,
                        detected_at_utc,
                        bot_name,
                        symbol,
                        short_id,
                        finding_type,
                        severity,
                        details,
                        suggested_action,
                        auto_heal_allowed,
                        resolved,
                        resolved_at_utc)
                    VALUES(@Id, @Run, @At, @Bot, @Symbol, @Short, @Type, @Severity, @Details, @Action, @Auto, false, null);
                    """,
                    new
                    {
                        f.Id,
                        Run = runId,
                        At = f.DetectedAtUtc,
                        Bot = f.BotName,
                        f.Symbol,
                        Short = f.ShortId,
                        Type = f.Type.ToString(),
                        Severity = f.Severity.ToString(),
                        f.Details,
                        Action = f.SuggestedAction.ToString(),
                        Auto = f.AutoHealAllowed
                    },
                    transaction,
                    cancellationToken: ct));

                continue;
            }

            var canonicalId = existingIds[0];

            await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE trading.reconciliation_findings
                SET run_id = @Run,
                    detected_at_utc = @At,
                    severity = @Severity,
                    details = @Details,
                    suggested_action = @Action,
                    auto_heal_allowed = @Auto
                WHERE id = @Id;
                """,
                new
                {
                    Id = canonicalId,
                    Run = runId,
                    At = f.DetectedAtUtc,
                    Severity = f.Severity.ToString(),
                    f.Details,
                    Action = f.SuggestedAction.ToString(),
                    Auto = f.AutoHealAllowed
                },
                transaction,
                cancellationToken: ct));

            if (existingIds.Count > 1)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE trading.reconciliation_findings
                    SET resolved = true,
                        resolved_at_utc = COALESCE(resolved_at_utc, @ResolvedAt)
                    WHERE id = ANY(@DuplicateIds);
                    """,
                    new
                    {
                        DuplicateIds = existingIds.Skip(1).ToArray(),
                        ResolvedAt = result.CompletedAtUtc
                    },
                    transaction,
                    cancellationToken: ct));
            }
        }

        if (result.EvaluatedSymbols.Count > 0)
        {
            var unresolved = (await connection.QueryAsync<UnresolvedFindingRow>(new CommandDefinition(
                """
                SELECT 
                    id,
                    bot_name AS BotName,
                    symbol AS Symbol,
                    short_id AS ShortId,
                    finding_type AS FindingType
                FROM trading.reconciliation_findings
                WHERE NOT resolved
                  AND symbol = ANY(@Symbols);
                """,
                new { Symbols = result.EvaluatedSymbols.ToArray() },
                transaction,
                cancellationToken: ct))).AsList();

            var idsToResolve = unresolved
                .Where(r => !currentFingerprints.Contains(Fingerprint(r.BotName, r.Symbol, r.ShortId, r.FindingType)))
                .Select(r => r.Id)
                .ToArray();

            if (idsToResolve.Length > 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE trading.reconciliation_findings
                    SET 
                        resolved = true,
                        resolved_at_utc = COALESCE(resolved_at_utc, @ResolvedAt)
                    WHERE id = ANY(@Ids);
                    """,
                    new
                    {
                        Ids = idsToResolve,
                        ResolvedAt = result.CompletedAtUtc
                    },
                    transaction,
                    cancellationToken: ct));
            }
        }

        await transaction.CommitAsync(ct);
    }

    public async Task<bool> HasUnresolvedCriticalAsync(CancellationToken ct)
    {
        await using var c = await connections.OpenAsync(ct);

        return await c.ExecuteScalarAsync<bool>(new CommandDefinition(
            """
            SELECT EXISTS(
                SELECT 1
                FROM trading.reconciliation_findings
                WHERE NOT resolved
                  AND severity = 'Critical');
            """,
            cancellationToken: ct));
    }

    private static string Fingerprint(
        string botName,
        string symbol,
        string? shortId,
        string findingType)
        => string.Join(
            "|",
            botName.Trim().ToUpperInvariant(),
            symbol.Trim().ToUpperInvariant(),
            shortId?.Trim().ToUpperInvariant() ?? string.Empty,
            findingType.Trim().ToUpperInvariant());
}
