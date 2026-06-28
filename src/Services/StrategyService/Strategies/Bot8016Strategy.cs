using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Models;
using StrategyService.Services;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies;

public sealed class Bot8016Strategy
{
    private const string BotName = "BOT8016";

    private readonly Bot8016Options _options;
    private readonly PositionManager _positions;
    private readonly OrderExecutionService _orders;
    private readonly ILogger<Bot8016Strategy> _logger;

    public Bot8016Strategy(
        IOptions<Bot8016Options> options,
        PositionManager positions,
        OrderExecutionService orders,
        ILogger<Bot8016Strategy> logger)
    {
        _options = options.Value;
        _positions = positions;
        _orders = orders;
        _logger = logger;
    }

    public async Task<bool> ProcessSignalAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(signal.Action, out var side))
            return false;

        var sameSideCount = await _positions.CountBySideAsync(BotName, side, cancellationToken);

        if (sameSideCount >= _options.PositionSideLimit)
            return false;

        var position = await _orders.OpenStop3TrailingPositionAsync(
            BotName,
            side,
            _options.Symbol,
            _options.Quantity,
            _options.InitialStopLoss,
            _options.TakeProfitPercent,
            cancellationToken);

        await _positions.AddAsync(position, cancellationToken);

        _logger.LogInformation(
            "{BotName} indicator signal processed. Side={Side}, PositionId={PositionId}",
            BotName,
            side,
            position.ShortId);

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