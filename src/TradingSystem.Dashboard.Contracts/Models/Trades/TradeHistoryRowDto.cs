namespace TradingSystem.Dashboard.Contracts.Models.Trades;

public sealed record TradeHistoryRowDto(string PositionId, string BotName, string Symbol, string Side, decimal? EntryPrice, decimal? ExitPrice, decimal Quantity, decimal? RealizedPnl, decimal? Fees, TimeSpan? Duration, string? Source, string StrategyVersion, string Environment, string? CloseReason, DateTime OpenedAtUtc, DateTime? ClosedAtUtc);

