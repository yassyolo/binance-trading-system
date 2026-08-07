using TradingSystem.Reconciliation.Executor;
using TradingSystem.Reconciliation.Models;
using TradingSystem.Reconciliation.Models.Enums;

namespace StrategyService.Reliability;

public sealed class SafeHealingActionExecutor(
    ILogger<SafeHealingActionExecutor> logger) 
    : IHealingActionExecutor
{
    public Task<bool> ExecuteAsync(ReconciliationFinding finding, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (finding.SuggestedAction != HealingActionType.DeleteStaleLocalPosition || finding.ShortId is null)
            return Task.FromResult(false);

        logger.LogWarning("Automatic healing refused destructive local-position deletion. Bot = {Bot}, Position = {Position}, Finding = {FindingId}. Manual review is required.", finding.BotName, finding.ShortId, finding.Id);

        return Task.FromResult(false);
    }
}
