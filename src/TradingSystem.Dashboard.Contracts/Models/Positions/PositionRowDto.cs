namespace TradingSystem.Dashboard.Contracts.Models.Positions;

public sealed record PositionRowDto(
    string PositionId, 
    string BotName, 
    string Symbol, 
    string Side, 
    string Status, 
    decimal Quantity, 
    decimal? EntryPrice, 
    decimal? TakeProfitPrice, 
    decimal? CurrentPrice, 
    decimal UnrealizedPnl,
    decimal? RealizedPnl, 
    DateTime OpenedAtUtc, 
    DateTime? ClosedAtUtc,
    string StrategyVersion, 
    string Environment);

