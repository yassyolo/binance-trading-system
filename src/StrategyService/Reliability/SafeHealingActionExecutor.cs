using Microsoft.Extensions.Options;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Reconciliation.Configuration;
using TradingSystem.Reconciliation.Contracts;
using TradingSystem.Reconciliation.Models;
using TradingSystem.Reconciliation.Models.Enums;
using TradingSystem.Reconciliation.Services;

namespace StrategyService.Reliability;

public sealed class SafeHealingActionExecutor(
    IPositionStore postionStore,
    IExchangeStateProvider exchangeStateProvider,
    LivePositionLifecycleRecorder positionLifecycleRecorder,
    IOptions<ReconciliationOptions> options,
    TimeProvider time,
    ILogger<SafeHealingActionExecutor> logger)
    : IHealingActionExecutor
{
    private readonly ReconciliationOptions _options = options.Value;

    public async Task<bool> ExecuteAsync(ReconciliationFinding finding, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (finding.SuggestedAction != HealingActionType.DeleteStaleLocalPosition || string.IsNullOrWhiteSpace(finding.ShortId))
            return false;

        var position = await postionStore.GetAsync(finding.BotName, finding.ShortId, ct);

        if (position is null)
        {
            logger.LogInformation("Stale local-position healing is already satisfied because the local position is absent. Bot = {Bot}, Position = {Position}, Finding = {FindingId}", finding.BotName, finding.ShortId, finding.Id);
            return true;
        }

        if (position.Closed)
        {
            logger.LogInformation("Stale local-position healing is already satisfied because the local position is closed. Bot = {Bot}, Position = {Position}, Finding = {FindingId}", finding.BotName, finding.ShortId, finding.Id);
            return true;
        }

        var remote = await exchangeStateProvider.GetAsync(position.Symbol, ct);

        var hasRemoteSidePosition = remote.Positions.Any(p => p.Symbol.Equals(position.Symbol, StringComparison.OrdinalIgnoreCase)
            && p.Side.Equals(position.Side.ToString(), StringComparison.OrdinalIgnoreCase) 
            && Math.Abs(p.Quantity) > _options.QuantityTolerance);
        if (hasRemoteSidePosition)
        {
            logger.LogWarning("Automatic stale-position healing refused because Binance exposure exists again. Bot = {Bot}, Position = {Position}, Symbol = {Symbol}", position.BotName, position.ShortId, position.Symbol);
            return false;
        }

        var knownClientIds = PositionReconciliationService.EnumerateClientOrderIds(position).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hasRelatedOpenOrder = remote.Orders.Any(o => !string.IsNullOrWhiteSpace(o.ClientOrderId) && knownClientIds.Contains(o.ClientOrderId));

        if (hasRelatedOpenOrder)
        {
            logger.LogWarning("Automatic stale-position healing refused because a related Binance order exists again. Bot = {Bot}, Position = {Position}, Symbol = {Symbol}", position.BotName, position.ShortId, position.Symbol);
            return false;
        }

        var now = time.GetUtcNow().UtcDateTime;
        position.MarkClosed("RECONCILIATION_STALE_LOCAL_POSITION", now);

        await postionStore.SaveAsync(position, ct);
       
        await positionLifecycleRecorder.RecordClosedAsync(position.BotName, position.ShortId, "RECONCILIATION_STALE_LOCAL_POSITION", ct);

        logger.LogWarning("Stale local position marked closed after Binance revalidation. Bot = {Bot}, Position = {Position}, Symbol = {Symbol}, Finding = {FindingId}", position.BotName, position.ShortId, position.Symbol, finding.Id);

        return true;
    }
}
