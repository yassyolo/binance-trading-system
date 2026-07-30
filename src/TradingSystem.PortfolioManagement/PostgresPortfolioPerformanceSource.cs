using Dapper;
using Microsoft.Extensions.Logging;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.PortfolioManagement;

public sealed class PostgresPortfolioPerformanceSource(
    ITradingDbConnectionFactory connectionFactory,
    ILogger<PostgresPortfolioPerformanceSource> logger) : IPortfolioPerformanceSource
{
    public async Task<PortfolioPerformanceSnapshot> GetAsync(
        decimal currentUnrealizedPnl,
        decimal startingEquity,
        DateTime asOfUtc,
        CancellationToken ct)
    {
        const string sql = """
            SELECT COALESCE(realized_pnl, 0)
            FROM trading_history.positions
            WHERE closed_at_utc >= @DayStartUtc
              AND closed_at_utc < @DayEndUtc
              AND closed_at_utc IS NOT NULL
            ORDER BY closed_at_utc;
            """;

        var dayStart = asOfUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        await using var connection = await connectionFactory.OpenAsync(ct);
        var closedPnls = (await connection.QueryAsync<decimal>(new CommandDefinition(
            sql,
            new { DayStartUtc = dayStart, DayEndUtc = dayEnd },
            commandTimeout: connectionFactory.CommandTimeoutSeconds,
            cancellationToken: ct))).AsList();

        var realizedToday = closedPnls.Sum();
        var runningEquity = startingEquity;
        var peakEquity = startingEquity;
        var consecutiveLosses = 0;

        foreach (var pnl in closedPnls)
        {
            runningEquity += pnl;
            peakEquity = Math.Max(peakEquity, runningEquity);
            consecutiveLosses = pnl < 0 ? consecutiveLosses + 1 : 0;
        }

        peakEquity = Math.Max(peakEquity, startingEquity + realizedToday + currentUnrealizedPnl);

        logger.LogDebug(
            "Portfolio performance loaded. RealizedToday = {RealizedToday}, ConsecutiveLosses = {ConsecutiveLosses}",
            realizedToday,
            consecutiveLosses);

        return new PortfolioPerformanceSnapshot(
            realizedToday,
            peakEquity,
            consecutiveLosses,
            asOfUtc);
    }
}