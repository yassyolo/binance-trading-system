using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using TradingSystem.Application.Healing.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Contracts.UserStream;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012HealingService(
    IOptions<Bot8012Options> options, 
    IPositionStore positionStore, 
    IClock clock, 
    ILogger<Bot8012HealingService> logger)
    :IBotHealingService
{
    private readonly Bot8012Options _option = options.Value;
    
    public string BotName => _option.BotName;
    
    public async Task HealAsync(HealingSnapshotMessage s, CancellationToken ct)
    {
        if(!_option.EnableHealing || !s.Symbol.Equals(_option.Symbol, StringComparison.OrdinalIgnoreCase))
            return;
        
        var active = s.ActiveClientIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        foreach(var p in (await positionStore.GetAllAsync(BotName, ct))
                                   .Where(x => !x.Closed && !string.IsNullOrWhiteSpace(x.TpClientId) 
                                    && !active.Contains(x.TpClientId!)))
        {
            p.MarkClosed("HEALING_ORDER_ABSENT", clock.UtcNow);
            
            await positionStore.SaveAsync(p, ct);
            
            logger.LogWarning("Position reconciled as closed. Bot = {Bot} Position = {Position}", BotName, p.ShortId);
        }
    }
}
