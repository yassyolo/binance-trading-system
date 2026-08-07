using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8011.Configuration;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8011;

public sealed class Bot8011Strategy(IOptions<Bot8011Options> options) : ITradingStrategy, IHasSignalCooldown
{
    readonly Bot8011Options o = options.Value;

    public StrategyMetadata Metadata => new(o.BotName, o.StrategyVersion, PositionMode.Hedge, [o.Symbol]);
    public TimeSpan SignalCooldown => TimeSpan.FromSeconds(o.CooldownSeconds);

    public Task<StrategyDecision> DecideAsync(StrategyContext c, CancellationToken ct)
    {
        var side = c.Signal.Side;
        if (side == PositionSide.Long && !o.EnableLong)
            return Task.FromResult(StrategyDecision.Block(side, "LONG is disabled."));
        if (side == PositionSide.Short && !o.EnableShort)
            return Task.FromResult(StrategyDecision.Block(side, "SHORT is disabled."));

        // FIX: Use correct comparison for init-only property
        var opposite = c.ActivePositions
            .Where(x => x.Side != side)
            .Select(x => x.ShortId)
            .ToArray();

        if (opposite.Length > 0)
            return Task.FromResult(StrategyDecision.OpenAfterClosing(side, opposite, $"Reverse signal: close {opposite.Length} opposite position(s)."));

        var same = c.ActivePositions.Count(x => x.Side == side);
        return Task.FromResult(
            same >= o.OrderSideLimit
                ? StrategyDecision.Block(side, $"ORDER_SIDE_LIMIT reached ({same}/{o.OrderSideLimit}).")
                : StrategyDecision.Open(side, $"BOT8011 accepted {side} signal.")
        );
    }
}
