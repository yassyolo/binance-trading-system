using TradingSystem.Reconciliation.Models;

namespace TradingSystem.Reconciliation.Contracts;

public interface IHealingActionExecutor
{
    Task<bool> ExecuteAsync(ReconciliationFinding finding, CancellationToken ct);
}
