using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Healing;

namespace StrategyService.Services.Healing;

public sealed class Bot8011HealingService(
    IOptions<Bot8011Options> options,
    IPositionStore positionStore,
    Bot8011PositionEventService events,
    ILogger<Bot8011HealingService> logger)
    : IBotHealingService
{
    private readonly Bot8011Options _options = options.Value;

    public string BotName => _options.BotName;

    public async Task HealAsync(
        HealingSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!snapshot.Type.Equals(
                "healing_snapshot",
                StringComparison.OrdinalIgnoreCase)
            || !snapshot.Symbol.Equals(
                _options.Symbol,
                StringComparison.OrdinalIgnoreCase))
            return;

        var active = snapshot.ActiveClientIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var positions = await positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        foreach (var position in positions.Where(x => !x.Closed))
        {
            var tp = Present(active, position.TpClientId);
            var sl = Present(active, position.SlClientId);
            var stop3 = Present(active, position.Stop3ClientId);

            if (!position.TpExecuted && tp && sl)
                continue;

            if (!position.TpExecuted && !tp && sl)
            {
                await events.HandleTpFilledAsync(
                    position.ShortId,
                    position.Quantity / 2m,
                    cancellationToken);
                continue;
            }

            if (!position.TpExecuted && tp && !sl)
            {
                await events.HandleSlTriggeredAsync(
                    position.ShortId,
                    cancellationToken);
                continue;
            }

            if (position.TpExecuted
                && position.Stop3Created
                && position.ProtectiveActive
                && !stop3)
            {
                await events.HandleStop3TriggeredAsync(
                    position.ShortId,
                    cancellationToken);
                continue;
            }

            if (!tp && !sl && !stop3)
            {
                // Preserve history instead of deleting Redis data.
                position.ProtectiveActive = false;
                position.Stop3Pending = false;
                position.TrailingInProgress = false;
                position.MarkClosed("HEALING_NO_ACTIVE_ORDERS");

                await positionStore.SaveAsync(position, cancellationToken);

                logger.LogWarning(
                    "BOT8011 healing closed orphan state. Position={ShortId}",
                    position.ShortId);
            }
        }
    }

    private static bool Present(
        HashSet<string> active,
        string? clientId)
        => !string.IsNullOrWhiteSpace(clientId)
           && active.Contains(clientId);
}
