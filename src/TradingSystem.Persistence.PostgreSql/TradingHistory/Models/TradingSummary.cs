namespace TradingSystem.Persistence.PostgreSql.TradingHistory.Models;

public sealed record TradingSummary(
    long Signals, 
    long OpenDecisions, 
    long BlockedDecisions,
    long Positions, 
    long ClosedPositions,
    decimal RealizedPnl);

