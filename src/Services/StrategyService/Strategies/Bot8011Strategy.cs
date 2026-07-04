using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace StrategyService.Strategies;

public sealed class Bot8011Strategy(
    IOptions<Bot8011Options> options,
    PositionManager positionManager,
    OrderExecutionService orders,
    ILogger<Bot8011Strategy> logger) 
    : IBotStrategy
{
    private readonly Bot8011Options options = options.Value;
    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = [];

    public string BotName => options.BotName.ToLowerInvariant();

    public async Task<bool> ProcessSignalAsync(TradingSignal signal, CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(signal.Action, out var side))
        {
            logger.LogWarning("Invalid BOT8011 signal action. Action={Action}", signal.Action);
            return false;
        }

        if (!IsSideEnabled(side))
        {
            logger.LogInformation("BOT8011 {Side} signal ignored because side is disabled.", side);
            return false;
        }

        if (IsInCooldown(side))
        {
            logger.LogInformation("BOT8011 {Side} signal ignored because of cooldown.", side);
            return false;
        }

        await CloseOppositePositionsAsync(side, cancellationToken);

        var sameSideCount = await positionManager.CountBySideAsync(options.BotName, side, cancellationToken);

        if (sameSideCount >= options.OrderSideLimit)
        {
            logger.LogInformation("BOT8011 signal blocked. Side={Side}, Count={Count}, Limit={Limit}", side, sameSideCount, options.OrderSideLimit);

            return false;
        }

        var position = await orders.OpenBot8011PositionAsync(side, options, cancellationToken);

        await positionManager.AddAsync(position, cancellationToken);

        _lastSignalAt[side] = DateTime.UtcNow;

        logger.LogInformation("BOT8011 signal processed successfully. Side={Side}, PositionId={PositionId}", side, position.ShortId);

        return true;
    }

    private async Task CloseOppositePositionsAsync(PositionSide side, CancellationToken cancellationToken)
    {
        var oppositePositions = await positionManager.GetOppositeAsync(options.BotName, side, cancellationToken);

        foreach (var oppositePosition in oppositePositions)
        {
            await orders.ClosePositionAsync(options.BotName, oppositePosition, cancellationToken);

            await positionManager.MarkClosedAsync(oppositePosition, cancellationToken);
        }
    }

    private bool IsSideEnabled(PositionSide side)
    {
        return side switch
        {
            PositionSide.Long => options.EnableLong,
            PositionSide.Short => options.EnableShort,
            _ => false
        };
    }

    private bool IsInCooldown(PositionSide side)
    {
        if (!_lastSignalAt.TryGetValue(side, out var lastSignalAt))
            return false;

        return DateTime.UtcNow - lastSignalAt < TimeSpan.FromSeconds(options.CooldownSeconds);
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