using TradingSystem.PortfolioManagement.Models;

namespace TradingSystem.PortfolioManagement.Position;

public interface IPaperPortfolioPositionSource
{
    Task<IReadOnlyCollection<PaperPortfolioPosition>> GetOpenAsync(CancellationToken ct);
}