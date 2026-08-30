namespace TradingSystem.Observability.History.Models;

public sealed record SignalHistoryRecord(
    string SignalId,
    string BotName,
    string StrategyVersion,
    string Symbol,
    string Side,
    string Source,
    string Environment,
    DateTime SignalTimeUtc,
    decimal? ReferencePrice,
    DateTime? CandleOpenTimeUtc,
    string? Interval,
    string? Reason,
    string? RawPayload,
    IReadOnlyDictionary<string, object?>? Metadata = null);
