using TradingSystem.Reconciliation.Models;

namespace TradingSystem.Reconciliation.Contracts;

public interface IReconciliationFindingStore
{
    Task SaveRunAsync(ReconciliationRunResult result, CancellationToken ct);
    Task<bool> HasUnresolvedCriticalAsync(CancellationToken ct);
}
