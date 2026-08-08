using TradingSystem.Application.Risk.Contracts;
using TradingSystem.Application.Risk.Models;

namespace TradingSystem.RiskManagement.Services;

public sealed class EmptyRiskStateProvider : IRiskStateProvider
{
    public Task<RiskStateSnapshot> GetAsync(DateTime atUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        return Task.FromResult(new RiskStateSnapshot(0m, 0m, 0m, 0, false));
    }
}
