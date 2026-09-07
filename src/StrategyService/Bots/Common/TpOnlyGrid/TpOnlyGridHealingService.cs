using Microsoft.Extensions.Logging;
using TradingSystem.Application.Healing.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Contracts.UserStream;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public abstract class TpOnlyGridHealingService<TOptions>(
    TOptions options,
    IPositionStore positionStore,
    IClock clock) 
    : IBotHealingService where TOptions : class, ITpOnlyGridBotOptions
{
    public string BotName => options.BotName;

    public async Task HealAsync(HealingSnapshotMessage snapshot, CancellationToken ct)
    {
        if (!options.EnableHealing || !snapshot.Symbol.Equals(options.Symbol, StringComparison.OrdinalIgnoreCase))
            return;

        var positions = await positionStore.GetAllAsync(BotName, ct);

        var positionsWithMissingTakeProfit = positions.Where(p => !p.Closed && !string.IsNullOrWhiteSpace(p.TpClientId) 
            && !snapshot.ActiveClientIds.ToHashSet(StringComparer.OrdinalIgnoreCase).Contains(p.TpClientId));

        foreach (var position in positionsWithMissingTakeProfit)
        {
            position.MarkClosed("HEALING_TP_MISSING", clock.UtcNow);
            
            await positionStore.SaveAsync(position, ct);
        }
    }
}
