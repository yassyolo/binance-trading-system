using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Strategies.Bot8014;

public sealed class Bot8014Strategy(
    IOptions<Bot8014Options> options,
    Bot8014GapPolicy gapPolicy)
    : ITradingStrategy,
      IHasSignalCooldown
{
    private readonly Bot8014Options _options = options.Value;

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
                StrategyDecision.Block(side, "LONG is disabled."));
        }

        if (side == PositionSide.Short && !_options.EnableShort)
        {
            return Task.FromResult(
                StrategyDecision.Block(side, "SHORT is disabled."));
        }

        return Task.FromResult(
            gapPolicy.Validate(
                side,
                context.MarkPrice,
                context.ActivePositions));
    }
}
