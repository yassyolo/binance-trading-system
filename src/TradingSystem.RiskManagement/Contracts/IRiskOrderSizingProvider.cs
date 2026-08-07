using TradingSystem.RiskManagement.Models;

namespace TradingSystem.RiskManagement.Contracts;

public interface IRiskOrderSizingProvider
{
    Task<RiskOrderSize?> GetAsync(string botName, CancellationToken ct);
}
