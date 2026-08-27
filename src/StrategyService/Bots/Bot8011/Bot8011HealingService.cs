using Microsoft.Extensions.Options;
using TradingSystem.Contracts.UserStream;
using TradingSystem.Application.Time;
using StrategyService.Bots.Bot8011.Configuration;
using TradingSystem.Application.Healing.Contracts;
using TradingSystem.Application.Positions.Contracts;

namespace StrategyService.Bots.Bot8011;

public sealed class Bot8011HealingService(
    IOptions<Bot8011Options> options, 
    IPositionStore store, IClock clock)
    :IBotHealingService
{
    readonly Bot8011Options options = options.Value;
    public string BotName => options.BotName;
    
    public async Task HealAsync(HealingSnapshotMessage healingSnapshot, CancellationToken ct)
    {
        if(!healingSnapshot.Symbol.Equals(options.Symbol, StringComparison.OrdinalIgnoreCase))
            return;
        
        var active = healingSnapshot.ActiveClientIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        foreach(var p in (await store.GetAllAsync(BotName, ct)).Where(x => !x.Closed))
        {
            var any = new[]
            {
                p.TpClientId, 
                p.SlClientId, 
                p.Stop3ClientId
            }.Any(x => !string.IsNullOrWhiteSpace(x) && active.Contains(x));
            
            if(!any && !p.Stop3Pending)
            {
                p.ProtectiveActive = false;           
                p.MarkClosed("HEALING_NO_ACTIVE_ORDERS", clock.UtcNow);
                
                await store.SaveAsync(p, ct);
            }
        }
    }
}
