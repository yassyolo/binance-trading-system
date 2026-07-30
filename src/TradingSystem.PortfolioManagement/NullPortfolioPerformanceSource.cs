namespace TradingSystem.PortfolioManagement;

public sealed class NullPortfolioPerformanceSource : IPortfolioPerformanceSource
{
    public Task<PortfolioPerformanceSnapshot> GetAsync(decimal currentUnrealizedPnl, decimal startingEquity, DateTime asOfUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var equity = startingEquity + currentUnrealizedPnl;

        return Task.FromResult(PortfolioPerformanceSnapshot.Empty(asOfUtc, equity));
    }
}
