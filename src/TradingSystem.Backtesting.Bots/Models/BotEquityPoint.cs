namespace TradingSystem.Backtesting.Bots.Models;

public sealed record BotEquityPoint(
    DateTime TimeUtc,
    decimal Balance,
    decimal Equity,
    decimal Peak,
    decimal DrawdownAmount,
    decimal DrawdownPercent);