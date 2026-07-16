namespace TradingSystem.Signals.Models;

public sealed record GeneratedTradingSignal(
    string SignalId,
    string BotName,
    string StrategyVersion,
    string Symbol,
    string Side,
    string Source,
    DateTime SignalTimeUtc,
    DateTime? CandleOpenTimeUtc,
    string? Interval,
    decimal ReferencePrice,
    string Reason,
    IReadOnlyDictionary<string, object?> Metadata);
