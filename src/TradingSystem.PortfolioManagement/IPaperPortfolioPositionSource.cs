namespace TradingSystem.PortfolioManagement;

public interface IPaperPortfolioPositionSource
{
    Task<IReadOnlyCollection<PaperPortfolioPosition>> GetOpenAsync(
        CancellationToken ct);
}