namespace TradingSystem.PaperTrading.Models;

public sealed record PaperTradingAccount(
    decimal InitialBalance,
    decimal RealizedPnl,
    decimal Fees,
    decimal Equity,
    int OpenPositions,
    int ClosedPositions,
    DateTime CalculatedAtUtc);
