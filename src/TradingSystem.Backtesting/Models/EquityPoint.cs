namespace TradingSystem.Backtesting.Models;

public sealed record EquityPoint(DateTime TimeUtc,  decimal Balance,  decimal DrawdownAmount,  decimal DrawdownPercent);
