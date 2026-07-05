using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace StrategyService.Strategies.Bot8011;

public sealed class Bot8011Strategy(
    IOptions<Bot8011Options> options,
    PositionManager positionManager,
    Bot8011EffectivePositionService effectivePositions,
    OrderExecutionService orders,
    ILogger<Bot8011Strategy> logger)
    : IBotStrategy
{
    private readonly Bot8011Options options = options.Value;
    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = [];

    public string BotName => options.BotName.ToLowerInvariant();

    public async Task<bool> ProcessSignalAsync(
        TradingSignal signal,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(signal.Action, out var side))
            return false;

        if (!IsSideEnabled(side))
            return false;

        if (IsInCooldown(side))
            return false;

        await CloseOppositePositionsAsync(side, cancellationToken);

        var sameSideCount = await effectivePositions.CountBySideAsync(
            side,
            cancellationToken);

        if (sameSideCount >= options.OrderSideLimit)
            return false;

        var position = await orders.OpenBot8011PositionAsync(
            side,
            options,
            cancellationToken);

        await positionManager.AddAsync(position, cancellationToken);

        _lastSignalAt[side] = DateTime.UtcNow;

        logger.LogInformation(
            "BOT8011 signal processed. Side={Side}, Position={Position}",
            side,
            position.ShortId);

        return true;
    }

    private async Task CloseOppositePositionsAsync(
        PositionSide side,
        CancellationToken cancellationToken)
    {
        var oppositePositions = await effectivePositions.GetOppositeAsync(
            side,
            cancellationToken);

        foreach (var oppositePosition in oppositePositions)
        {
            await orders.ClosePositionAsync(
                options.BotName,
                oppositePosition,
                cancellationToken);

            await positionManager.MarkClosedAsync(
                oppositePosition,
                "OPPOSITE_SIGNAL",
                cancellationToken);
        }
    }

    private bool IsSideEnabled(PositionSide side)
        => side == PositionSide.Long
            ? options.EnableLong
            : options.EnableShort;

    private bool IsInCooldown(PositionSide side)
    {
        if (!_lastSignalAt.TryGetValue(side, out var lastSignalAt))
            return false;

        return DateTime.UtcNow - lastSignalAt <
               TimeSpan.FromSeconds(options.CooldownSeconds);
    }

    private static bool TryParseSide(string action, out PositionSide side)
    {
        side = default;

        if (action.Equals("long", StringComparison.OrdinalIgnoreCase))
        {
            side = PositionSide.Long;
            return true;
        }

        if (action.Equals("short", StringComparison.OrdinalIgnoreCase))
        {
            side = PositionSide.Short;
            return true;
        }

        return false;
    }
}