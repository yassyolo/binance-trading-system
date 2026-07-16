using Dapper;
using Npgsql;

namespace TradingSystem.Persistence.PostgreSql.TradingHistory;

public sealed class TradingHistoryQueryService(string connectionString)
{
    public async Task<IReadOnlyCollection<SignalListItem>> GetRecentSignalsAsync(
        string? botName, string? environment, int limit, CancellationToken ct)
    {
        const string sql = """
        SELECT signal_id AS SignalId, bot_name AS BotName, strategy_version AS StrategyVersion,
               symbol AS Symbol, side AS Side, source AS Source, environment AS Environment,
               signal_time_utc AS SignalTimeUtc, reference_price AS ReferencePrice, reason AS Reason
        FROM trading_history.signals
        WHERE (@BotName IS NULL OR bot_name = @BotName)
          AND (@Environment IS NULL OR environment = @Environment)
        ORDER BY signal_time_utc DESC
        LIMIT @Limit;
        """;
        return await QueryAsync<SignalListItem>(sql, new
        {
            BotName = botName,
            Environment = environment,
            Limit = Math.Clamp(limit, 1, 1000)
        }, ct);
    }

    public async Task<TradingSummary> GetSummaryAsync(
        string botName, string environment, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        const string sql = """
        SELECT
          COUNT(*) AS Signals,
          COUNT(*) FILTER (WHERE d.decision = 'Open') AS OpenDecisions,
          COUNT(*) FILTER (WHERE d.decision = 'Block') AS BlockedDecisions,
          COUNT(DISTINCT p.position_id) AS Positions,
          COUNT(DISTINCT p.position_id) FILTER (WHERE p.status = 'Closed') AS ClosedPositions,
          COALESCE(SUM(DISTINCT p.realized_pnl) FILTER (WHERE p.status = 'Closed'), 0) AS RealizedPnl
        FROM trading_history.signals s
        LEFT JOIN trading_history.strategy_decisions d ON d.signal_id = s.signal_id
        LEFT JOIN trading_history.positions p ON p.signal_id = s.signal_id
        WHERE s.bot_name = @BotName
          AND s.environment = @Environment
          AND s.signal_time_utc >= @FromUtc
          AND s.signal_time_utc < @ToUtc;
        """;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return await connection.QuerySingleAsync<TradingSummary>(
            new CommandDefinition(sql, new { botName, environment, fromUtc, toUtc }, cancellationToken: ct));
    }

    public async Task<IReadOnlyCollection<BlockReasonSummary>> GetBlockReasonsAsync(
        string botName, string environment, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        const string sql = """
        SELECT COALESCE(reason, 'Unknown') AS Reason, COUNT(*) AS Count
        FROM trading_history.strategy_decisions
        WHERE bot_name = @BotName AND environment = @Environment
          AND decision = 'Block'
          AND decided_at_utc >= @FromUtc AND decided_at_utc < @ToUtc
        GROUP BY COALESCE(reason, 'Unknown')
        ORDER BY Count DESC;
        """;
        return await QueryAsync<BlockReasonSummary>(sql, new { botName, environment, fromUtc, toUtc }, ct);
    }

    private async Task<IReadOnlyCollection<T>> QueryAsync<T>(string sql, object args, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<T>(new CommandDefinition(sql, args, cancellationToken: ct));
        return rows.AsList();
    }
}

public sealed record SignalListItem(
    string SignalId, string BotName, string StrategyVersion, string Symbol,
    string Side, string Source, string Environment, DateTime SignalTimeUtc,
    decimal? ReferencePrice, string? Reason);
public sealed record TradingSummary(long Signals, long OpenDecisions, long BlockedDecisions,
    long Positions, long ClosedPositions, decimal RealizedPnl);
public sealed record BlockReasonSummary(string Reason, long Count);
