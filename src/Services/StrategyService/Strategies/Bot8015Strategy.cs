using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Models;
using StrategyService.Services;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies;

public sealed class Bot8015Strategy
{
    private const string BotName = "BOT8015";

    private readonly Bot8015Options _options;
    private readonly PositionManager _positionManager;
    private readonly OrderExecutionService _orders;
    private readonly ILogger<Bot8015Strategy> _logger;

    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = new();

    public Bot8015Strategy(
        IOptions<Bot8015Options> options,
        PositionManager positions,
        OrderExecutionService orders,
        ILogger<Bot8015Strategy> logger)
    {
        _options = options.Value;
        _positionManager = positions;
        _orders = orders;
        _logger = logger;
    }

    public async Task<bool> ProcessSignalAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(signal.Action, out var side))
            return false;

        if (IsInCooldown(side))
            return false;

        var oppositePositions = await _positionManager.GetOppositeAsync(BotName, side, cancellationToken);

        foreach (var opposite in oppositePositions)
        {
            await _orders.ClosePositionAsync(BotName, opposite, cancellationToken);
            await _positionManager.MarkClosedAsync(
                opposite,
                cancellationToken);
        }

        var sameSideCount = await _positionManager.CountBySideAsync(BotName, side, cancellationToken);

        if (sameSideCount >= _options.OrderSideLimit)
            return false;

        var position = await _orders.OpenStop3TrailingPositionAsync(
            BotName,
            side,
            _options.Symbol,
            _options.Quantity,
            _options.InitialStopLoss,
            _options.TakeProfitPercent,
            cancellationToken);

        await _positionManager.AddAsync(position, cancellationToken);

        _lastSignalAt[side] = DateTime.UtcNow;

        _logger.LogInformation(
            "{BotName} signal processed. Side={Side}, PositionId={PositionId}",
            BotName,
            side,
            position.ShortId);

        return true;
    }

    private bool IsInCooldown(PositionSide side)
        => _lastSignalAt.TryGetValue(side, out var last) &&
           DateTime.UtcNow - last < TimeSpan.FromSeconds(_options.CooldownSeconds);

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