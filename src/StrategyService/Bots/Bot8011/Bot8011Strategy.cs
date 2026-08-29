using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8011.Configuration;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8011;

public sealed class Bot8011Strategy(
    IOptions<Bot8011Options> options) 
    : ITradingStrategy, IHasSignalCooldown
{
    readonly Bot8011Options _options = options.Value;

    public StrategyMetadata Metadata 
        => new(_options.BotName, _options.StrategyVersion, PositionMode.Hedge, [_options.Symbol]);
    public TimeSpan SignalCooldown => TimeSpan.FromSeconds(_options.CooldownSeconds);

    public Task<StrategyDecision> DecideAsync(StrategyContext c, CancellationToken ct)
    {
        var side = c.Signal.Side;
        
        if (side == PositionSide.Long && !_options.EnableLong)
            return Task.FromResult(StrategyDecision.Block(side, "LONG is disabled."));
       
        if (side == PositionSide.Short && !_options.EnableShort)
            return Task.FromResult(StrategyDecision.Block(side, "SHORT is disabled."));

        var opposite = c.ActivePositions.Where(x => x.Side != side)
            .Select(x => x.ShortId)
            .ToArray();

        if (opposite.Length > 0)
            return Task.FromResult(StrategyDecision.OpenAfterClosing(side, opposite, $"Reverse signal: close {opposite.Length} opposite position(s)."));

        var same = c.ActivePositions.Count(x => x.Side == side);
        return Task.FromResult(
            same >= _options.OrderSideLimit
                ? StrategyDecision.Block(side, $"ORDER_SIDE_LIMIT reached ({same}/{_options.OrderSideLimit}).")
                : StrategyDecision.Open(side, $"BOT8011 accepted {side} signal.")
        );
    }
}
