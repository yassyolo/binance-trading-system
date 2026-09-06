using TradingSystem.PortfolioManagement.Models;

namespace TradingSystem.PortfolioManagement.Position;

public sealed class NullPaperPortfolioPositionSource : IPaperPortfolioPositionSource
{
    public Task<IReadOnlyCollection<PaperPortfolioPosition>> GetOpenAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
       
        return Task.FromResult<IReadOnlyCollection<PaperPortfolioPosition>>([]);
    }
}