using TradingSystem.Observability.Abstractions;
using TradingSystem.Observability.Models;
namespace TradingSystem.Observability.Services;

public sealed class TradingHistoryRecorder(ITradingHistoryStore store)
{
    public Task RecordAsync(TradingHistoryEventType type, string botName, string version, string symbol,
        DateTime occurredAtUtc, string? side = null, string? signalId = null, string? positionId = null,
        string? source = null, string? decision = null, string? reason = null, decimal? markPrice = null,
        decimal? price = null, decimal? quantity = null, decimal? fee = null, decimal? pnl = null,
        string? rawPayload = null, IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
        => store.AppendAsync(new TradingHistoryEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc),
            Type = type,
            BotName = botName,
            StrategyVersion = version,
            Symbol = symbol,
            Side = side,
            SignalId = signalId,
            PositionId = positionId,
            CorrelationId = signalId,
            Source = source,
            Decision = decision,
            Reason = reason,
            MarkPrice = markPrice,
            Price = price,
            Quantity = quantity,
            Fee = fee,
            RealizedPnl = pnl,
            RawPayload = rawPayload,
            Metadata = metadata
        }, cancellationToken);
}
