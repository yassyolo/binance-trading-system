using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace StrategyService.Strategies;

public sealed class Bot8016Strategy(
    IOptions<Bot8016Options> options,
    PositionManager positionManager,
    OrderExecutionService orders,
    ILogger<Bot8016Strategy> logger)
    : IBotStrategy
{
    public string BotName => "bot8016";

    private readonly Bot8016Options options = options.Value;
    public async Task<bool> ProcessSignalAsync(TradingSignal signal, CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(signal.Action, out var side))
            return false;

        var sameSideCount = await positionManager.CountBySideAsync(BotName, side, cancellationToken);

        if (sameSideCount >= options.PositionSideLimit)
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

        logger.LogInformation("{BotName} indicator signal processed. Side={Side}, PositionId={PositionId}", BotName, side, position.ShortId);

        return true;
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