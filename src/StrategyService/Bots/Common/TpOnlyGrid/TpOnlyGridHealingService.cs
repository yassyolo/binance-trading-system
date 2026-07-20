using TradingSystem.Application.Healing;using TradingSystem.Application.Positions;using TradingSystem.Application.Time;using TradingSystem.Contracts.UserStream;
namespace StrategyService.Bots.Common.TpOnlyGrid;
public abstract class TpOnlyGridHealingService<TOptions>(TOptions options, IPositionStore store, IClock clock):IBotHealingService where TOptions:class, ITpOnlyGridBotOptions
{
 public string BotName => options.BotName;
 public async Task HealAsync(HealingSnapshotMessage snapshot, CancellationToken ct){if(!options.EnableHealing || !snapshot.Symbol.Equals(options.Symbol, StringComparison.OrdinalIgnoreCase))return;var active = snapshot.ActiveClientIds.ToHashSet(StringComparer.OrdinalIgnoreCase);foreach(var p in (await store.GetAllAsync(BotName, ct)).Where(x => !x.Closed && !string.IsNullOrWhiteSpace(x.TpClientId) && !active.Contains(x.TpClientId!))){p.MarkProtectionMissing("HEALING_TP_MISSING", clock.UtcNow);await store.SaveAsync(p, ct);}}
}
