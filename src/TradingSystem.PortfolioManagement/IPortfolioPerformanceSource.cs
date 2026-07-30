namespace TradingSystem.PortfolioManagement;

public interface IPortfolioPerformanceSource
{
    Task<PortfolioPerformanceSnapshot> GetAsync(
        decimal currentUnrealizedPnl,
        decimal startingEquity,
        DateTime asOfUtc,
        CancellationToken ct);
}