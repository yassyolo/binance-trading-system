namespace TradingSystem.Dashboard.Contracts.Models.Analytics;

public sealed record RunSummaryDto(
    Guid RunId, 
    string RunType, 
    string BotName, 
    string StrategyVersion, 
    string Symbol, 
    string Interval, 
    DateTime StartedAtUtc, 
    DateTime? CompletedAtUtc, 
    string Status, 
    decimal? NetProfit, 
    decimal? WinRatePercent, 
    decimal? MaxDrawdownPercent, 
    decimal? Score);