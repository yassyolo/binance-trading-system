using System.Text.Json;
using Dapper;
using TradingSystem.Observability.History;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.TradingHistory;

public sealed class PostgresTradingPipelineRecorder(
    ITradingDbConnectionFactory factory)
    : ITradingPipelineRecorder
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task RecordSignalAsync(SignalHistoryRecord x, CancellationToken ct) => Exec(
        """
        insert into trading_history.signals
            (signal_id, bot_name, strategy_version, symbol, side, source, environment,
             signal_time_utc, reference_price, candle_open_time_utc, interval, reason,
             raw_payload, metadata)
        values
            (@SignalId, @BotName, @StrategyVersion, @Symbol, @Side, @Source, @Environment,
             @SignalTimeUtc, @ReferencePrice, @CandleOpenTimeUtc, @Interval, @Reason,
             cast(@Raw as jsonb), cast(@Meta as jsonb))
        on conflict(signal_id) do nothing;
        """,
        new
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
            Raw = Normalize(x.RawPayload),
            Meta = Serialize(x.Metadata)
        },
        ct);

    public Task RecordDecisionAsync(DecisionHistoryRecord x, CancellationToken ct) => Exec(
        """
        insert into trading_history.strategy_decisions
            (signal_id, bot_name, strategy_version, symbol, side, decision, reason,
             environment, decided_at_utc, mark_price, parameters, metadata)
        values
            (@SignalId, @BotName, @StrategyVersion, @Symbol, @Side, @Decision, @Reason,
             @Environment, @DecidedAtUtc, @MarkPrice, cast(@Params as jsonb), cast(@Meta as jsonb));
        """,
        new
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
            Params = Serialize(x.Parameters),
            Meta = Serialize(x.Metadata)
        },
        ct);

    public Task UpsertPositionAsync(PositionHistoryRecord x, CancellationToken ct) => Exec(
        """
        insert into trading_history.positions
            (position_id, signal_id, bot_name, strategy_version, symbol, side, source,
             environment, status, quantity, entry_price, take_profit_price, opened_at_utc,
             closed_at_utc, realized_pnl, fees, close_reason, metadata)
        values
            (@PositionId, @SignalId, @BotName, @StrategyVersion, @Symbol, @Side, @Source,
             @Environment, @Status, @Quantity, @EntryPrice, @TakeProfitPrice, @OpenedAtUtc,
             @ClosedAtUtc, @RealizedPnl, @Fees, @CloseReason, cast(@Meta as jsonb))
        on conflict(position_id) do update set
            signal_id = coalesce(excluded.signal_id, trading_history.positions.signal_id),
            strategy_version = case
                when excluded.strategy_version is not null and excluded.strategy_version <> 'unknown'
                    then excluded.strategy_version
                else trading_history.positions.strategy_version
            end,
            source = coalesce(excluded.source, trading_history.positions.source),
            environment = excluded.environment,
            status = excluded.status,
            quantity = excluded.quantity,
            entry_price = coalesce(excluded.entry_price, trading_history.positions.entry_price),
            take_profit_price = coalesce(excluded.take_profit_price, trading_history.positions.take_profit_price),
            opened_at_utc = least(excluded.opened_at_utc, trading_history.positions.opened_at_utc),
            closed_at_utc = excluded.closed_at_utc,
            realized_pnl = excluded.realized_pnl,
            fees = excluded.fees,
            close_reason = excluded.close_reason,
            metadata = trading_history.positions.metadata || excluded.metadata;
        """,
        new
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
            Meta = Serialize(x.Metadata)
        },
        ct);

    public Task RecordPositionEventAsync(PositionEventHistoryRecord x, CancellationToken ct) => Exec(
        """
        insert into trading_history.position_events
            (position_id, bot_name, event_type, status, occurred_at_utc, price, quantity, details)
        values
            (@PositionId, @BotName, @EventType, @Status, @OccurredAtUtc, @Price, @Quantity,
             cast(@Details as jsonb));
        """,
        new
        {
            x.PositionId,
            x.BotName,
            x.EventType,
            x.Status,
            x.OccurredAtUtc,
            x.Price,
            x.Quantity,
            Details = Serialize(x.Details)
        },
        ct);

    public Task RecordOrderEventAsync(OrderEventHistoryRecord x, CancellationToken ct) => Exec(
        """
        insert into trading_history.order_events
            (event_key, bot_name, position_id, client_order_id, exchange_order_id, order_type,
             status, side, symbol, environment, occurred_at_utc, price, quantity,
             executed_quantity, raw_payload)
        values
            (@EventKey, @BotName, @PositionId, @ClientOrderId, @ExchangeOrderId, @OrderType,
             @Status, @Side, @Symbol, @Environment, @OccurredAtUtc, @Price, @Quantity,
             @ExecutedQuantity, cast(@Raw as jsonb))
        on conflict(event_key) do nothing;
        """,
        new
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
            Raw = Normalize(x.RawPayload)
        },
        ct);

    private async Task Exec(string sql, object args, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            args,
            commandTimeout: factory.CommandTimeoutSeconds,
            cancellationToken: ct));
    }

    private static string Serialize(object? value) => JsonSerializer.Serialize(value ?? new { }, Json);

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "{}";

        try
        {
            using var _ = JsonDocument.Parse(value);
            return value;
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { value }, Json);
        }
    }
}
