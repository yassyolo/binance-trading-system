using TradingSystem.PortfolioManagement.Models;

namespace TradingSystem.PortfolioManagement.Provider;

public interface IPortfolioSnapshotProvider
{
    Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken ct);
    
    void Invalidate();
}