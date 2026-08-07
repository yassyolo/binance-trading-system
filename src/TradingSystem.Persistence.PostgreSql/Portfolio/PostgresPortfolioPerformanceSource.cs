using Dapper;
using Microsoft.Extensions.Logging;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.PortfolioManagement.Models;
using TradingSystem.PortfolioManagement.Performance;

namespace TradingSystem.Persistence.PostgreSql.Portfolio;

public sealed class PostgresPortfolioPerformanceSource(
    ITradingDbConnectionFactory connectionFactory,
    ILogger<PostgresPortfolioPerformanceSource> logger)
    : IPortfolioPerformanceSource
{
    public async Task<PortfolioPerformanceSnapshot> GetAsync(
        decimal currentUnrealizedPnl,
        decimal startingEquity,
        DateTime asOfUtc,
        CancellationToken ct)
    {
        const string sql = """
            select coalesce(realized_pnl, 0)
            from trading_history.positions
            where closed_at_utc >= @DayStartUtc
              and closed_at_utc < @DayEndUtc
              and closed_at_utc is not null
            order by closed_at_utc;
            """;

        var dayStartUtc = asOfUtc.Date;
        var dayEndUtc = dayStartUtc.AddDays(1);

        await using var connection =
            await connectionFactory.OpenAsync(ct);

        var closedPnls = (
            await connection.QueryAsync<decimal>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        DayStartUtc = dayStartUtc,
                        DayEndUtc = dayEndUtc
                    },
                    commandTimeout:
                        connectionFactory.CommandTimeoutSeconds,
                    cancellationToken: ct)))
            .AsList();

        var realizedToday = closedPnls.Sum();
        var runningEquity = startingEquity;
        var peakEquity = startingEquity;
        var consecutiveLosses = 0;

        foreach (var pnl in closedPnls)
        {
            runningEquity += pnl;

            peakEquity = Math.Max(
                peakEquity,
                runningEquity);

            consecutiveLosses = pnl < 0
                ? consecutiveLosses + 1
                : 0;
        }

        var currentEquity =
            startingEquity +
            realizedToday +
            currentUnrealizedPnl;

        peakEquity = Math.Max(
            peakEquity,
            currentEquity);

        logger.LogDebug(
            "Portfolio performance loaded. " +
            "RealizedToday = {RealizedToday}, " +
            "ConsecutiveLosses = {ConsecutiveLosses}",
            realizedToday,
            consecutiveLosses);

        return new PortfolioPerformanceSnapshot(
            realizedToday,
            peakEquity,
            consecutiveLosses,
            asOfUtc);
    }
}