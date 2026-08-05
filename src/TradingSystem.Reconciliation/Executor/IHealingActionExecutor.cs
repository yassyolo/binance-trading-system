namespace TradingSystem.Reconciliation.Executor;

public interface IHealingActionExecutor
{
    Task<bool> ExecuteAsync(ReconciliationFinding finding, CancellationToken ct);
}
