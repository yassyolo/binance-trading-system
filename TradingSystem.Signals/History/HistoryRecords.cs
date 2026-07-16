namespace TradingSystem.Signals.History;

public sealed record SignalReceivedRecord(
    string SignalId, string BotName, string StrategyVersion, string Symbol,
    string Side, string Source, string Environment, DateTime SignalTimeUtc,
    decimal? ReferencePrice, string? RawPayload, DateTime? CandleOpenTimeUtc = null,
    string? Interval = null, string? Reason = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed record StrategyDecisionRecord(
    string SignalId, string BotName, string StrategyVersion, string Symbol,
    string Side, string Decision, string? Reason, string Environment,
    DateTime DecidedAtUtc, decimal? MarkPrice,
    IReadOnlyDictionary<string, object?> Parameters,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed record PositionHistoryRecord(
    string PositionId, string? SignalId, string BotName, string StrategyVersion,
    string Symbol, string Side, string? Source, string Environment, string Status,
    decimal Quantity, decimal? EntryPrice, decimal? TakeProfitPrice,
    DateTime OpenedAtUtc, DateTime? ClosedAtUtc, decimal? RealizedPnl,
    decimal? Fees, string? CloseReason,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed record PositionEventRecord(
    string PositionId, string BotName, string EventType, string? Status,
    DateTime OccurredAtUtc, decimal? Price, decimal? Quantity,
    IReadOnlyDictionary<string, object?>? Details = null);

public sealed record OrderEventRecord(
    string EventKey, string BotName, string? PositionId, string? ClientOrderId,
    string? ExchangeOrderId, string? OrderType, string? Status, string? Side,
    string? Symbol, string Environment, DateTime OccurredAtUtc, decimal? Price,
    decimal? Quantity, decimal? ExecutedQuantity, string? RawPayload);
