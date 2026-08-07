namespace TradingSystem.Observability.History.Models;

public sealed record PositionEventHistoryRecord(
    string PositionId,
    string BotName,
    string EventType,
    string? Status,
    DateTime OccurredAtUtc,
    decimal? Price,
    decimal? Quantity,
    IReadOnlyDictionary<string, object?>? Details = null);
