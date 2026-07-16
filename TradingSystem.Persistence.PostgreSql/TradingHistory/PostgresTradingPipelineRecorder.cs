using System.Text.Json;
using Dapper;
using Npgsql;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.History;

namespace TradingSystem.Persistence.PostgreSql.TradingHistory;

public sealed class PostgresTradingPipelineRecorder(string connectionString)
    : ITradingPipelineRecorder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task RecordSignalReceivedAsync(SignalReceivedRecord x, CancellationToken ct)
    {
        const string sql = """
        INSERT INTO trading_history.signals
        (signal_id, bot_name, strategy_version, symbol, side, source, environment,
         signal_time_utc, reference_price, candle_open_time_utc, interval, reason,
         raw_payload, metadata)
        VALUES
        (@SignalId, @BotName, @StrategyVersion, @Symbol, @Side, @Source, @Environment,
         @SignalTimeUtc, @ReferencePrice, @CandleOpenTimeUtc, @Interval, @Reason,
         CAST(@RawPayload AS jsonb), CAST(@Metadata AS jsonb))
        ON CONFLICT (signal_id) DO NOTHING;
        """;

        await ExecuteAsync(sql, new
        {
            x.SignalId,
            x.BotName,
            x.StrategyVersion,
            x.Symbol,
            x.Side,
            x.Source,
            x.Environment,
            x.SignalTimeUtc,
            x.ReferencePrice,
            x.CandleOpenTimeUtc,
            x.Interval,
            x.Reason,
            RawPayload = NormalizeJson(x.RawPayload),
            Metadata = Serialize(x.Metadata)
        }, ct);
    }

    public Task RecordDecisionAsync(StrategyDecisionRecord x, CancellationToken ct)
        => ExecuteAsync("""
        INSERT INTO trading_history.strategy_decisions
        (signal_id, bot_name, strategy_version, symbol, side, decision, reason,
         environment, decided_at_utc, mark_price, parameters, metadata)
        VALUES
        (@SignalId, @BotName, @StrategyVersion, @Symbol, @Side, @Decision, @Reason,
         @Environment, @DecidedAtUtc, @MarkPrice, CAST(@Parameters AS jsonb), CAST(@Metadata AS jsonb));
        """, new
        {
            x.SignalId,
            x.BotName,
            x.StrategyVersion,
            x.Symbol,
            x.Side,
            x.Decision,
            x.Reason,
            x.Environment,
            x.DecidedAtUtc,
            x.MarkPrice,
            Parameters = Serialize(x.Parameters),
            Metadata = Serialize(x.Metadata)
        }, ct);

    public Task UpsertPositionAsync(PositionHistoryRecord x, CancellationToken ct)
        => ExecuteAsync("""
        INSERT INTO trading_history.positions
        (position_id, signal_id, bot_name, strategy_version, symbol, side, source,
         environment, status, quantity, entry_price, take_profit_price, opened_at_utc,
         closed_at_utc, realized_pnl, fees, close_reason, metadata)
        VALUES
        (@PositionId, @SignalId, @BotName, @StrategyVersion, @Symbol, @Side, @Source,
         @Environment, @Status, @Quantity, @EntryPrice, @TakeProfitPrice, @OpenedAtUtc,
         @ClosedAtUtc, @RealizedPnl, @Fees, @CloseReason, CAST(@Metadata AS jsonb))
        ON CONFLICT (position_id) DO UPDATE SET
          status = EXCLUDED.status,
          entry_price = COALESCE(EXCLUDED.entry_price, trading_history.positions.entry_price),
          take_profit_price = COALESCE(EXCLUDED.take_profit_price, trading_history.positions.take_profit_price),
          closed_at_utc = EXCLUDED.closed_at_utc,
          realized_pnl = EXCLUDED.realized_pnl,
          fees = EXCLUDED.fees,
          close_reason = EXCLUDED.close_reason,
          metadata = COALESCE(EXCLUDED.metadata, trading_history.positions.metadata);
        """, new
        {
            x.PositionId,
            x.SignalId,
            x.BotName,
            x.StrategyVersion,
            x.Symbol,
            x.Side,
            x.Source,
            x.Environment,
            x.Status,
            x.Quantity,
            x.EntryPrice,
            x.TakeProfitPrice,
            x.OpenedAtUtc,
            x.ClosedAtUtc,
            x.RealizedPnl,
            x.Fees,
            x.CloseReason,
            Metadata = Serialize(x.Metadata)
        }, ct);

    public Task RecordPositionEventAsync(PositionEventRecord x, CancellationToken ct)
        => ExecuteAsync("""
        INSERT INTO trading_history.position_events
        (position_id, bot_name, event_type, status, occurred_at_utc, price, quantity, details)
        VALUES
        (@PositionId, @BotName, @EventType, @Status, @OccurredAtUtc, @Price, @Quantity,
         CAST(@Details AS jsonb));
        """, new
        {
            x.PositionId,
            x.BotName,
            x.EventType,
            x.Status,
            x.OccurredAtUtc,
            x.Price,
            x.Quantity,
            Details = Serialize(x.Details)
        }, ct);

    public Task RecordOrderEventAsync(OrderEventRecord x, CancellationToken ct)
        => ExecuteAsync("""
        INSERT INTO trading_history.order_events
        (event_key, bot_name, position_id, client_order_id, exchange_order_id,
         order_type, status, side, symbol, environment, occurred_at_utc, price,
         quantity, executed_quantity, raw_payload)
        VALUES
        (@EventKey, @BotName, @PositionId, @ClientOrderId, @ExchangeOrderId,
         @OrderType, @Status, @Side, @Symbol, @Environment, @OccurredAtUtc, @Price,
         @Quantity, @ExecutedQuantity, CAST(@RawPayload AS jsonb))
        ON CONFLICT (event_key) DO NOTHING;
        """, new
        {
            x.EventKey,
            x.BotName,
            x.PositionId,
            x.ClientOrderId,
            x.ExchangeOrderId,
            x.OrderType,
            x.Status,
            x.Side,
            x.Symbol,
            x.Environment,
            x.OccurredAtUtc,
            x.Price,
            x.Quantity,
            x.ExecutedQuantity,
            RawPayload = NormalizeJson(x.RawPayload)
        }, ct);

    private async Task ExecuteAsync(string sql, object args, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct));
    }

    private static string Serialize(object? value)
        => JsonSerializer.Serialize(value ?? new { }, JsonOptions);

    private static string NormalizeJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "{}";
        try { using var _ = JsonDocument.Parse(raw); return raw; }
        catch (JsonException) { return JsonSerializer.Serialize(new { value = raw }, JsonOptions); }
    }
}
