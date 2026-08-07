namespace TradingSystem.Observability.History.Models;

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
