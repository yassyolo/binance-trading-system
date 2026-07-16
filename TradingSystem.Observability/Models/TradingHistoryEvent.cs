namespace TradingSystem.Observability.Models;

public enum TradingHistoryEventType
{
    SignalReceived,
    StrategyDecision,
    OrderSubmitted,
    OrderFilled,
    OrderRejected,
    PositionOpened,
    TakeProfitCreated,
    TakeProfitFilled,
    PositionClosed,
    HealingAction,
    Error
}

public sealed record TradingHistoryEvent
{
    public required string EventId { get; init; }
    public required DateTime OccurredAtUtc { get; init; }
    public required TradingHistoryEventType Type { get; init; }
    public required string BotName { get; init; }
    public required string StrategyVersion { get; init; }
    public required string Symbol { get; init; }
    public string? Side { get; init; }
    public string? SignalId { get; init; }
    public string? PositionId { get; init; }
    public string? CorrelationId { get; init; }
    public string? Source { get; init; }
    public string? Decision { get; init; }
    public string? Reason { get; init; }
    public decimal? MarkPrice { get; init; }
    public decimal? Price { get; init; }
    public decimal? Quantity { get; init; }
    public decimal? Fee { get; init; }
    public decimal? RealizedPnl { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public string? RawPayload { get; init; }
}
