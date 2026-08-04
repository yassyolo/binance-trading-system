namespace TradingSystem.Observability.History;

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

public sealed record PositionHistoryRecord(
    string PositionId, 
    string? SignalId, 
    string BotName, 
    string StrategyVersion, 
    string Symbol, 
    string Side, 
    string? Source, string Environment, 
    string Status, 
    decimal Quantity, decimal? EntryPrice, 
    decimal? TakeProfitPrice, 
    DateTime OpenedAtUtc, 
    DateTime? ClosedAtUtc,
    decimal? RealizedPnl, 
    decimal? Fees, 
    string? CloseReason, 
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed record PositionEventHistoryRecord(
    string PositionId, 
    string BotName, 
    string EventType, 
    string? Status, 
    DateTime OccurredAtUtc, 
    decimal? Price, 
    decimal? Quantity, 
    IReadOnlyDictionary<string, object?>? Details = null);

public sealed record OrderEventHistoryRecord(
    string EventKey, 
    string BotName, 
    string? PositionId, 
    string? ClientOrderId, 
    string? ExchangeOrderId, 
    string? OrderType, 
    string? Status, 
    string? Side, 
    string? Symbol, 
    string Environment, 
    DateTime OccurredAtUtc, 
    decimal? Price, 
    decimal? Quantity, 
    decimal? ExecutedQuantity, 
    string? RawPayload);
