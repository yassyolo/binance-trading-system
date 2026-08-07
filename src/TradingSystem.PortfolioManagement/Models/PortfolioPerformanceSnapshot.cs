namespace TradingSystem.PortfolioManagement.Models;

public sealed record PortfolioPerformanceSnapshot(
    decimal RealizedPnlToday,
    decimal PeakEquityToday,
    int ConsecutiveLosses,
    DateTime AsOfUtc)
{
    public static PortfolioPerformanceSnapshot Empty(DateTime asOfUtc, decimal currentEquity) =>
        new(0m, currentEquity, 0, asOfUtc);
}
