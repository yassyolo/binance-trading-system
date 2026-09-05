using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public abstract class TpOnlyGridStrategy<TOptions>(
    TOptions options, 
    TpOnlyGridGapPolicy<TOptions> tpGridPolicy):
    ITradingStrategy, 
    IHasSignalCooldown where TOptions : class, 
    ITpOnlyGridBotOptions
{
    public StrategyMetadata Metadata 
        => new(options.BotName, options.StrategyVersion, PositionMode.TpOnly, [options.Symbol]);
    
    public TimeSpan SignalCooldown => TimeSpan.FromSeconds(options.CooldownSeconds);
    
    public Task<StrategyDecision> DecideAsync(StrategyContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        var side = ctx.Signal.Side;
        
        if(side == PositionSide.Long && !options.EnableLong)
            return Task.FromResult(StrategyDecision.Block(side, "LONG is disabled."));
        
        if(side == PositionSide.Short && !options.EnableShort)
            return Task.FromResult(StrategyDecision.Block(side, "SHORT is disabled."));
        
        return Task.FromResult(tpGridPolicy.Evaluate(side, ctx.MarkPrice, ctx.ActivePositions, ctx.RuntimeConfiguration));
    }
}
