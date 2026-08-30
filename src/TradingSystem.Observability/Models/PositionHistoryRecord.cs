namespace TradingSystem.Observability.History.Models;

public sealed record PositionHistoryRecord(
    string PositionId, 
    string? SignalId, 
    string BotName, 
    string StrategyVersion, 
    string Symbol, 
    string Side, 
    string? Source, 
    string Environment, 
    string Status, 
    decimal Quantity, 
    decimal? EntryPrice, 
    decimal? TakeProfitPrice, 
    DateTime OpenedAtUtc, 
    DateTime? ClosedAtUtc,
    decimal? RealizedPnl, 
    decimal? Fees, 
    string? CloseReason, 
    IReadOnlyDictionary<string, object?>? Metadata = null);