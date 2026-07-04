using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace StrategyService.Strategies;

public sealed class Bot8015Strategy(
    IOptions<Bot8015Options> options,
    PositionManager positionManager,
    OrderExecutionService orders,
    ILogger<Bot8015Strategy> logger)
    : IBotStrategy
{
    public string BotName => "bot8015";

    private readonly Bot8015Options options = options.Value;
    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = new();

    public async Task<bool> ProcessSignalAsync(TradingSignal signal, CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(signal.Action, out var side))
            return false;

        if (IsInCooldown(side))
            return false;

        var oppositePositions = await positionManager.GetOppositeAsync(BotName, side, cancellationToken);

        foreach (var opposite in oppositePositions)
        {
            await orders.ClosePositionAsync(BotName, opposite, cancellationToken);
            await positionManager.MarkClosedAsync(opposite, cancellationToken);
        }

        var sameSideCount = await positionManager.CountBySideAsync(BotName, side, cancellationToken);

        if (sameSideCount >= options.OrderSideLimit)
            return false;

        var position = await orders.OpenStop3TrailingPositionAsync(
            BotName,
            side,
            options.Symbol,
            options.Quantity,
            options.InitialStopLoss,
            options.TakeProfitPercent,
            cancellationToken);

        await positionManager.AddAsync(position, cancellationToken);

        _lastSignalAt[side] = DateTime.UtcNow;

        logger.LogInformation("{BotName} signal processed. Side={Side}, PositionId={PositionId}", BotName, side, position.ShortId);

        return true;
    }

    private bool IsInCooldown(PositionSide side)
        => _lastSignalAt.TryGetValue(side, out var last) && DateTime.UtcNow - last < TimeSpan.FromSeconds(options.CooldownSeconds);

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