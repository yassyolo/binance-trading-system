namespace TradingSystem.Backtesting.Models;

public sealed record EquityPoint(
    DateTime TimeUtc,
    decimal Balance,
    decimal Equity,
    decimal PeakEquity,
    decimal DrawdownAmount,
    decimal DrawdownPercent);
