using TradingSystem.Application.Healing;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Time;
using TradingSystem.Contracts.UserStream;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public abstract class TpOnlyGridHealingService<TOptions>(
    TOptions options,
    IPositionStore store,
    IClock clock) : IBotHealingService
    where TOptions : class, ITpOnlyGridBotOptions
{
    public string BotName => options.BotName;

    public async Task HealAsync(HealingSnapshotMessage snapshot, CancellationToken ct)
    {
        if (!options.EnableHealing || !snapshot.Symbol.Equals(options.Symbol, StringComparison.OrdinalIgnoreCase))
            return;

        var activeClientIds = snapshot.ActiveClientIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var positions = await store.GetAllAsync(BotName, ct);

        var positionsWithMissingTakeProfit = positions.Where(position =>
            !position.Closed &&
            !string.IsNullOrWhiteSpace(position.TpClientId) &&
            !activeClientIds.Contains(position.TpClientId));

        foreach (var position in positionsWithMissingTakeProfit)
        {
            position.MarkClosed("HEALING_TP_MISSING", clock.UtcNow);
            
            await store.SaveAsync(position, ct);
        }
    }
}
