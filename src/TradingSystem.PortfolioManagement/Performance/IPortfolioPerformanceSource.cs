using TradingSystem.PortfolioManagement.Models;

namespace TradingSystem.PortfolioManagement.Performance;

public interface IPortfolioPerformanceSource
{
    Task<PortfolioPerformanceSnapshot> GetAsync(
        decimal currentUnrealizedPnl,
        decimal startingEquity,
        DateTime asOfUtc,
        CancellationToken ct);
}