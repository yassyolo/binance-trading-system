using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using TradingSystem.HistoricalDatabase.Configuration;
using TradingSystem.HistoricalDatabase.Models;

namespace TradingSystem.HistoricalDatabase.EventStore;

public sealed class PostgresHistoricalEventStore 
    : IHistoricalEventStore, IHistoricalEventSink
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;
    private readonly HistoricalDatabaseOptions _options;

    public PostgresHistoricalEventStore(
        IConfiguration configuration,
        IOptions<HistoricalDatabaseOptions> options)
    {
        _options = options.Value;
        _connectionString = configuration.GetConnectionString(_options.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{_options.ConnectionStringName}' was not found.");
    }

    public Task WriteAsync(HistoricalEvent historicalEvent, CancellationToken ct) 
        => AppendAsync(historicalEvent, ct);

    public async Task AppendAsync(HistoricalEvent historicalEvent, CancellationToken ct)
    {
        if (!_options.Enabled)
            return;

        await using var connection = new NpgsqlConnection(_connectionString);
       
        await connection.OpenAsync(ct);
        
        await using var command = CreateInsertCommand(connection, historicalEvent);
        
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct)
    {
        if (!_options.Enabled)
            return 0;

        const string sql = """
            WITH deleted AS (
                DELETE FROM trading_history.events
                WHERE occurred_at_utc < @cutoff
                RETURNING 1
            )
            SELECT COUNT(*) FROM deleted;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        
        await connection.OpenAsync(ct);
        
        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };  
        command.Parameters.AddWithValue("cutoff", cutoffUtc);
        
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    private NpgsqlCommand CreateInsertCommand(NpgsqlConnection connection, HistoricalEvent item, NpgsqlTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO trading_history.events
            (event_id, 
             event_type, 
             occurred_at_utc, 
             environment, 
             correlation_id,
             bot_name, 
             strategy_version, 
             symbol, 
             position_id, 
             order_id, 
             side,
             status,
             price,
             quantity, 
             realized_pnl,
             reason, 
             data, 
             raw_payload)
            VALUES
            (@event_id, @event_type, @occurred_at_utc, @environment, @correlation_id, @bot_name, @strategy_version, @symbol, @position_id, @order_id, @side, @status, @price, @quantity, @realized_pnl, @reason, CAST(@data AS jsonb), @raw_payload)
            ON CONFLICT (event_id) DO NOTHING;
            """;

        var command = new NpgsqlCommand(sql, connection, transaction)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };
        command.Parameters.AddWithValue("event_id", item.EventId);
        command.Parameters.AddWithValue("event_type", item.EventType.ToString());
        command.Parameters.AddWithValue("occurred_at_utc", item.OccurredAtUtc);
        command.Parameters.AddWithValue("environment", item.Environment);
        command.Parameters.AddWithValue("correlation_id", (object?)item.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("bot_name", (object?)item.BotName ?? DBNull.Value);
        command.Parameters.AddWithValue("strategy_version", (object?)item.StrategyVersion ?? DBNull.Value);
        command.Parameters.AddWithValue("symbol", (object?)item.Symbol ?? DBNull.Value);
        command.Parameters.AddWithValue("position_id", (object?)item.PositionId ?? DBNull.Value);
        command.Parameters.AddWithValue("order_id", (object?)item.OrderId ?? DBNull.Value);
        command.Parameters.AddWithValue("side", (object?)item.Side ?? DBNull.Value);
        command.Parameters.AddWithValue("status", (object?)item.Status ?? DBNull.Value);
        command.Parameters.AddWithValue("price", (object?)item.Price ?? DBNull.Value);
        command.Parameters.AddWithValue("quantity", (object?)item.Quantity ?? DBNull.Value);
        command.Parameters.AddWithValue("realized_pnl", (object?)item.RealizedPnl ?? DBNull.Value);
        command.Parameters.AddWithValue("reason", (object?)item.Reason ?? DBNull.Value);
        command.Parameters.AddWithValue("data", JsonSerializer.Serialize(item.Data, JsonOptions));
        command.Parameters.AddWithValue("raw_payload", _options.StoreRawPayloads ? (object?)item.RawPayload ?? DBNull.Value : DBNull.Value);
        
        return command;
    }
}