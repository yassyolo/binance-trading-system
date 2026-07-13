using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8012;

public sealed class Bot8012Strategy(
    IOptions<Bot8012Options> options,
    Bot8012GapPolicy gapPolicy)
    : ITradingStrategy,
      IHasSignalCooldown
{
    private readonly Bot8012Options _options = options.Value;

    public StrategyMetadata Metadata => new(
        _options.BotName,
        _options.StrategyVersion,
        PositionMode.TpOnly,
        [_options.Symbol]);

    public TimeSpan SignalCooldown
        => TimeSpan.FromSeconds(_options.CooldownSeconds);

    public Task<StrategyDecision> DecideAsync(
        StrategyContext context,
        CancellationToken cancellationToken)
    {
        var side = context.Signal.Side;

        if (side == PositionSide.Long && !_options.EnableLong)
        {
            return Task.FromResult(
                StrategyDecision.Block(
                    side,
                    "LONG is disabled."));
        }

        if (side == PositionSide.Short && !_options.EnableShort)
        {
            return Task.FromResult(
                StrategyDecision.Block(
                    side,
                    "SHORT is disabled."));
        }

        var decision = gapPolicy.Validate(
            side,
            context.MarkPrice,
            context.ActivePositions);

        return Task.FromResult(decision);
    }
}
