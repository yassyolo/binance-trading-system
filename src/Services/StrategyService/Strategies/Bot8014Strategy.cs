using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace StrategyService.Strategies;

public sealed class Bot8014Strategy(
    IOptions<Bot8014Options> options,
    PositionManager positions,
    OrderExecutionService orders,
    ILogger<Bot8014Strategy> logger)
    : IBotStrategy
{
    public string BotName => "bot8014";

    private readonly Bot8014Options options = options.Value;
    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = new();

    public async Task<bool> ProcessSignalAsync(TradingSignal signal, CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(signal.Action, out var side))
            return false;

        if (side == PositionSide.Long && !options.EnableLong)
            return false;

        if (side == PositionSide.Short && !options.EnableShort)
            return false;

        if (IsInCooldown(side))
        {
            logger.LogInformation("{BotName} {Side} signal ignored because of cooldown.", BotName, side);
            return false;
        }

        var sameSideCount = await positions.CountBySideAsync(BotName, side, cancellationToken);

        if (sameSideCount >= options.OrderSideLimit)
            return false;

        var position = await orders.OpenTpOnlyPositionAsync(
            BotName,
            side,
            options.Symbol,
            options.Quantity,
            options.ProfitDistance,
            cancellationToken);

        await positions.AddAsync(position, cancellationToken);

        _lastSignalAt[side] = DateTime.UtcNow;
        return true;
    }

    private bool IsInCooldown(PositionSide side) => _lastSignalAt.TryGetValue(side, out var last) && DateTime.UtcNow - last < TimeSpan.FromSeconds(options.CooldownSeconds);

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