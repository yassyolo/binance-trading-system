namespace TradingSystem.PortfolioManagement;

public interface IPortfolioSnapshotProvider
{
    Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken ct);
    void Invalidate();
}

public interface IPortfolioPerformanceSource
{
    Task<PortfolioPerformanceSnapshot> GetAsync(decimal currentUnrealizedPnl, decimal startingEquity, DateTime asOfUtc, CancellationToken ct);
}

public sealed class NullPortfolioPerformanceSource : IPortfolioPerformanceSource
{
    public Task<PortfolioPerformanceSnapshot> GetAsync(decimal currentUnrealizedPnl, decimal startingEquity, DateTime asOfUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
       
        var equity = startingEquity + currentUnrealizedPnl;
        
        return Task.FromResult(PortfolioPerformanceSnapshot.Empty(asOfUtc, equity));
    }
}