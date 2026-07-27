namespace TradingSystem.PortfolioManager;

public interface IPortfolioSnapshotProvider
{
    Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
    void Invalidate();
}

public interface IPortfolioPerformanceSource
{
    Task<PortfolioPerformanceSnapshot> GetAsync(
        decimal currentUnrealizedPnl,
        decimal startingEquity,
        DateTime asOfUtc,
        CancellationToken cancellationToken);
}

public sealed class NullPortfolioPerformanceSource : IPortfolioPerformanceSource
{
    public Task<PortfolioPerformanceSnapshot> GetAsync(
        decimal currentUnrealizedPnl,
        decimal startingEquity,
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var equity = startingEquity + currentUnrealizedPnl;
        return Task.FromResult(PortfolioPerformanceSnapshot.Empty(asOfUtc, equity));
    }
}
