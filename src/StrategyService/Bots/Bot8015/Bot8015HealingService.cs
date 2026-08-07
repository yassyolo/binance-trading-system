using Microsoft.Extensions.Options;
using TradingSystem.Contracts.UserStream;
using TradingSystem.Application.Time;
using StrategyService.Bots.Bot8015.Configuration;
using TradingSystem.Application.Healing.Contracts;
using TradingSystem.Application.Positions.Contracts;

namespace StrategyService.Bots.Bot8015;

public sealed class Bot8015HealingService(
    IOptions<Bot8015Options> options, 
    IPositionStore store, 
    IClock clock):
    IBotHealingService
{
    readonly Bot8015Options o = options.Value;
    public string BotName => o.BotName;
    
    public async Task HealAsync(HealingSnapshotMessage s, CancellationToken ct)
    {
        if(!s.Symbol.Equals(o.Symbol, StringComparison.OrdinalIgnoreCase))
            return;
        
        var active = s.ActiveClientIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        foreach(var p in (await store.GetAllAsync(BotName, ct)).Where(x => !x.Closed))
        {
            var any = new[]{p.TpClientId, p.SlClientId, p.Stop3ClientId}.Any(x => !string.IsNullOrWhiteSpace(x) && active.Contains(x));
            
            if(!any && !p.Stop3Pending)
            {
                p.ProtectiveActive = false;
                
                p.MarkClosed("HEALING_NO_ACTIVE_ORDERS", clock.UtcNow);
                
                await store.SaveAsync(p, ct);
            }
        }
    }
}
