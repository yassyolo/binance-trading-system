using TradingSystem.Application.Risk.Models;

namespace TradingSystem.Application.Risk.Contracts;

public interface IRiskStateProvider
{
    Task<RiskStateSnapshot> GetAsync(DateTime atUtc, CancellationToken ct);
}
