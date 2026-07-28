namespace TradingSystem.HistoricalDatabase;

public enum HistoricalEventType
{
    SignalReceived,
    StrategyDecision,
    ExecutionCompleted,
    ProcessingFailed,
    OrderUpdate,
    PositionChanged,
    TradeClosed,
    RuntimeCommand,
    ConfigurationChanged,
    HealingAction,
    ReconciliationFinding,
    PortfolioSnapshot
}

public sealed record HistoricalEvent(
    Guid EventId,
    HistoricalEventType EventType,
    DateTime OccurredAtUtc,
    string Environment,
    string? CorrelationId,
    string? BotName,
    string? StrategyVersion,
    string? Symbol,
    string? PositionId,
    string? OrderId,
    string? Side,
    string? Status,
    decimal? Price,
    decimal? Quantity,
    decimal? RealizedPnl,
    string? Reason,
    IReadOnlyDictionary<string, object?> Data,
    string? RawPayload = null);

public sealed record HistoricalTradeSummary(
    string PositionId,
    string BotName,
    string StrategyVersion,
    string Symbol,
    string Side,
    DateTime OpenedAtUtc,
    DateTime ClosedAtUtc,
    decimal Quantity,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal GrossPnl,
    decimal Commission,
    decimal NetPnl,
    string CloseReason,
    string Environment);
