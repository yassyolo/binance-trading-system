namespace TradingSystem.Persistence.PostgreSql.TradingHistory.Models;

public sealed record SignalListItem(
    string SignalId,
    string BotName, 
    string StrategyVersion, 
    string Symbol, 
    string Side, 
    string Source, 
    string Environment, 
    DateTime SignalTimeUtc,
    decimal? ReferencePrice, 
    string? Reason);

