namespace TradingSystem.HistoricalDatabase.Models;

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