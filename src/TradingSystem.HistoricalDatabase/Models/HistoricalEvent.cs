using TradingSystem.HistoricalDatabase.Models.Enums;

namespace TradingSystem.HistoricalDatabase.Models;

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
