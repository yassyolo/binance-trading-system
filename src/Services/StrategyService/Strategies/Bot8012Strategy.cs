using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace StrategyService.Strategies;

public sealed class Bot8012Strategy(
    IOptions<Bot8012Options> options,
    PositionManager positions,
    OrderExecutionService orders,
    ILogger<Bot8012Strategy> logger)
    : IBotStrategy
{
    public string BotName => "bot8012";

    private readonly Bot8012Options options = options.Value;
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
        {
            logger.LogInformation("{BotName} blocked. Side={Side}, Count={Count}, Limit={Limit}",BotName, side, sameSideCount, options.OrderSideLimit);

            return false;
        }

        var position = await orders.OpenTpOnlyPositionAsync(
            botName: BotName,
            side: side,
            symbol: options.Symbol,
            quantity: options.Quantity,
            profitDistance: options.ProfitDistance,
            cancellationToken: cancellationToken);

        await positions.AddAsync(position, cancellationToken);

        _lastSignalAt[side] = DateTime.UtcNow;

        logger.LogInformation("{BotName} signal processed. Side={Side}, PositionId={PositionId}", BotName, side, position.ShortId);

        return true;
    }

    private bool IsInCooldown(PositionSide side)
    {
        if (!_lastSignalAt.TryGetValue(side, out var last))
            return false;

        return DateTime.UtcNow - last < TimeSpan.FromSeconds(options.CooldownSeconds);
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