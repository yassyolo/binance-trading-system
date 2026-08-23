using Microsoft.Extensions.Options;
using StrategyService.Services;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Reconciliation.Configuration;
using TradingSystem.Reconciliation.Contracts;
using TradingSystem.Reconciliation.Models;
using TradingSystem.Reconciliation.Models.Enums;
using TradingSystem.Reconciliation.Services;

namespace StrategyService.Reliability;

public sealed class SafeHealingActionExecutor(
    IPositionStore localStore,
    IExchangeStateProvider exchange,
    LivePositionLifecycleRecorder lifecycle,
    IOptions<ReconciliationOptions> options,
    TimeProvider time,
    ILogger<SafeHealingActionExecutor> logger)
    : IHealingActionExecutor
{
    private readonly ReconciliationOptions _options = options.Value;

    public async Task<bool> ExecuteAsync(ReconciliationFinding finding, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (finding.SuggestedAction != HealingActionType.DeleteStaleLocalPosition ||
            string.IsNullOrWhiteSpace(finding.ShortId))
        {
            return false;
        }

        var position = await localStore.GetAsync(finding.BotName, finding.ShortId, ct);

        if (position is null)
        {
            logger.LogInformation(
                "Stale local-position healing is already satisfied because the local position is absent. Bot = {Bot}, Position = {Position}, Finding = {FindingId}",
                finding.BotName,
                finding.ShortId,
                finding.Id);

            return true;
        }

        if (position.Closed)
        {
            logger.LogInformation(
                "Stale local-position healing is already satisfied because the local position is closed. Bot = {Bot}, Position = {Position}, Finding = {FindingId}",
                finding.BotName,
                finding.ShortId,
                finding.Id);

            return true;
        }

        // Re-read Binance immediately before changing local state. The original finding
        // can be several seconds old and must not be treated as authorization to mutate.
        var remote = await exchange.GetAsync(position.Symbol, ct);

        var hasRemoteSidePosition = remote.Positions.Any(remotePosition =>
            remotePosition.Symbol.Equals(position.Symbol, StringComparison.OrdinalIgnoreCase) &&
            remotePosition.Side.Equals(position.Side.ToString(), StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(remotePosition.Quantity) > _options.QuantityTolerance);

        if (hasRemoteSidePosition)
        {
            logger.LogWarning(
                "Automatic stale-position healing refused because Binance exposure exists again. Bot = {Bot}, Position = {Position}, Symbol = {Symbol}",
                position.BotName,
                position.ShortId,
                position.Symbol);

            return false;
        }

        var knownClientIds = PositionReconciliationService
            .EnumerateClientOrderIds(position)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hasRelatedOpenOrder = remote.Orders.Any(order =>
            !string.IsNullOrWhiteSpace(order.ClientOrderId) &&
            knownClientIds.Contains(order.ClientOrderId));

        if (hasRelatedOpenOrder)
        {
            logger.LogWarning(
                "Automatic stale-position healing refused because a related Binance order exists again. Bot = {Bot}, Position = {Position}, Symbol = {Symbol}",
                position.BotName,
                position.ShortId,
                position.Symbol);

            return false;
        }

        var now = time.GetUtcNow().UtcDateTime;
        position.MarkClosed("RECONCILIATION_STALE_LOCAL_POSITION", now);

        await localStore.SaveAsync(position, ct);
        await lifecycle.RecordClosedAsync(
            position.BotName,
            position.ShortId,
            "RECONCILIATION_STALE_LOCAL_POSITION",
            ct);

        logger.LogWarning(
            "Stale local position marked closed after Binance revalidation. Bot = {Bot}, Position = {Position}, Symbol = {Symbol}, Finding = {FindingId}",
            position.BotName,
            position.ShortId,
            position.Symbol,
            finding.Id);

        return true;
    }
}
