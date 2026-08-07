namespace TradingSystem.Observability.History.Models;

public sealed record DecisionHistoryRecord(
    string SignalId,
    string BotName,
    string StrategyVersion,
    string Symbol,
    string Side,
    string Decision,
    string? Reason,
    string Environment,
    DateTime DecidedAtUtc,
    decimal? MarkPrice,
    IReadOnlyDictionary<string, object?>? Parameters = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);
