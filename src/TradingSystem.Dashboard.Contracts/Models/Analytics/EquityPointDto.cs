namespace TradingSystem.Dashboard.Contracts.Models.Analytics;

public sealed record EquityPointDto(
    DateTime TimeUtc, 
    decimal Equity, 
    decimal DrawdownPercent);
