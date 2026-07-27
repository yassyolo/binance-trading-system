using Dapper;
using TradingSystem.Application.Risk;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.Reconciliation;

namespace TradingSystem.Persistence.PostgreSql.Reliability;

public sealed class PostgresRiskStateProvider(
    ITradingDbConnectionFactory connections,
    IReconciliationFindingStore findings) : IRiskStateProvider
{
    public async Task<RiskStateSnapshot> GetAsync(
        DateTime atUtc,
        CancellationToken ct)
    {
        var normalizedAtUtc = atUtc.Kind == DateTimeKind.Utc
            ? atUtc
            : atUtc.ToUniversalTime();

        const string sql = """
            with day_bounds as (
                select date_trunc('day', @AtUtc) as day_start,
                       date_trunc('day', @AtUtc) + interval '1 day' as day_end
            ),
            closed_today as (
                select realized_pnl, closed_at_utc
                from trading_history.positions, day_bounds
                where status = 'Closed'
                  and closed_at_utc >= day_start
                  and closed_at_utc < day_end
            ),
            ordered_outcomes as (
                select realized_pnl,
                       sum(case when realized_pnl >= 0 then 1 else 0 end)
                           over(order by closed_at_utc desc
                                rows between unbounded preceding and current row) as wins_seen
                from closed_today
            ),
            equity as (
                select coalesce(max(s.final_balance), 0) as peak,
                       coalesce((array_agg(s.final_balance order by r.completed_at_utc desc))[1], 0) as current
                from trading.performance_snapshots s
                join trading.performance_runs r on r.run_id = s.run_id
                cross join day_bounds
                where r.run_type = 'Live'
                  and r.completed_at_utc >= day_start
                  and r.completed_at_utc < day_end
            )
            select
                coalesce((select sum(realized_pnl) from closed_today), 0) DailyRealizedPnl,
                equity.peak DailyPeakEquity,
                equity.current CurrentEquity,
                coalesce((
                    select count(*)::int
                    from ordered_outcomes
                    where wins_seen = 0 and realized_pnl < 0
                ), 0) ConsecutiveLosses
            from equity;
            """;

        await using var connection = await connections.OpenAsync(ct);
        var row = await connection.QuerySingleAsync<RiskRow>(
            new CommandDefinition(
                sql,
                new { AtUtc = normalizedAtUtc },
                commandTimeout: connections.CommandTimeoutSeconds,
                cancellationToken: ct));

        var hasCriticalFindings = await findings.HasUnresolvedCriticalAsync(ct);

        return new RiskStateSnapshot(
            row.DailyRealizedPnl,
            row.DailyPeakEquity,
            row.CurrentEquity,
            row.ConsecutiveLosses,
            hasCriticalFindings);
    }

    private sealed record RiskRow(
        decimal DailyRealizedPnl,
        decimal DailyPeakEquity,
        decimal CurrentEquity,
        int ConsecutiveLosses);
}
