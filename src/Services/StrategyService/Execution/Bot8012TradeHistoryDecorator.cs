using TradingSystem.Application.Execution;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;
using TradingSystem.Observability.Models;
using TradingSystem.Observability.Services;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.History;

namespace StrategyService.Execution;

// Register this instead of the raw Bot8012TradeExecutor when you want execution history.
public sealed class Bot8012TradeHistoryDecorator(Bot8012TradeExecutor inner, TradingHistoryRecorder history) : IBotTradeExecutor
{
    public string BotName => inner.BotName;
    public async Task<TradeExecutionResult> OpenAsync(string symbol, PositionSide side, string? source, CancellationToken ct) { var result = await inner.OpenAsync(symbol, side, source, ct); await history.RecordAsync(result.Succeeded ? TradingHistoryEventType.PositionOpened : TradingHistoryEventType.OrderRejected, BotName, "1.0.0", symbol, DateTime.UtcNow, side.ToString(), positionId: result.ShortId, source: source, decision: result.Succeeded ? "Success" : "Failure", reason: result.Reason, cancellationToken: ct); return result; }
    public async Task<TradeExecutionResult> CloseAsync(string shortId, string reason, CancellationToken ct) { var result = await inner.CloseAsync(shortId, reason, ct); await history.RecordAsync(TradingHistoryEventType.PositionClosed, BotName, "1.0.0", "BTCUSDC", DateTime.UtcNow, positionId: shortId, decision: result.Succeeded ? "Success" : "Failure", reason: reason, cancellationToken: ct); return result; }

    public static Task RecordOpenedAsync(
        ITradingPipelineRecorder recorder,
        dynamic position,
        string? signalId,
        string strategyVersion,
        string environment,
        CancellationToken ct)
        => recorder.UpsertPositionAsync(new PositionHistoryRecord(
            position.ShortId,
            signalId,
            position.BotName,
            strategyVersion,
            position.Symbol,
            position.Side.ToString(),
            position.Source,
            environment,
            "Open",
            position.Quantity,
            position.EntryPrice,
            position.TpPrice,
            position.CreatedAtUtc,
            null,
            null,
            null,
            null), ct);
}
