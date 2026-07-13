using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Strategies.Bot8015;

public sealed class Bot8015Strategy(
    IOptions<Bot8015Options> options)
    : ITradingStrategy,
      IHasSignalCooldown
{
    private readonly Bot8015Options _options = options.Value;

    public StrategyMetadata Metadata => new(
        _options.BotName,
        _options.StrategyVersion,
        PositionMode.Stop3,
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

        var oppositeSide = side == PositionSide.Long
            ? PositionSide.Short
            : PositionSide.Long;

        var oppositePositionIds = context.ActivePositions
            .Where(x => x.Side == oppositeSide)
            .Select(x => x.ShortId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (oppositePositionIds.Length > 0)
        {
            return Task.FromResult(
                StrategyDecision.OpenAfterClosing(
                    side,
                    oppositePositionIds,
                    $"Close {oppositePositionIds.Length} opposite {oppositeSide} position(s) first."));
        }

        var sameSideCount = context.ActivePositions.Count(x => x.Side == side);

        if (sameSideCount >= _options.OrderSideLimit)
        {
            return Task.FromResult(
                StrategyDecision.Block(
                    side,
                    $"LIMIT_REACHED {sameSideCount}/{_options.OrderSideLimit}."));
        }

        return Task.FromResult(
            StrategyDecision.Open(
                side,
                $"BOT8015 can open. Active same-side positions: {sameSideCount}."));
    }
}
