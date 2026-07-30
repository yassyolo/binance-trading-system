namespace TradingSystem.PortfolioManagement;

public interface IPortfolioSnapshotProvider
{
    Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken ct);
    void Invalidate();
}