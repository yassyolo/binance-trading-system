using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Models;
using StrategyService.Services;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies;

public sealed class Bot8011Strategy
{
    private readonly Bot8011Options _options;
    private readonly PositionManager _positionManager;
    private readonly OrderExecutionService _orders;
    private readonly ILogger<Bot8011Strategy> _logger;

    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = new();

    public Bot8011Strategy(
        IOptions<Bot8011Options> options,
        PositionManager positionManager,
        OrderExecutionService orders,
        ILogger<Bot8011Strategy> logger)
    {
        _options = options.Value;
        _positionManager = positionManager;
        _orders = orders;
        _logger = logger;
    }

    public async Task<bool> ProcessSignalAsync(
        Signal signal,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(signal.Action, out var side))
        {
            _logger.LogWarning("Invalid BOT8011 signal action: {Action}", signal.Action);
            return false;
        }

        if (side == PositionSide.Long && !_options.EnableLong)
            return false;

        if (side == PositionSide.Short && !_options.EnableShort)
            return false;

        if (IsInCooldown(side))
        {
            _logger.LogInformation("BOT8011 {Side} signal ignored because of cooldown.", side);
            return false;
        }

        var active = await _positionManager.GetActivePositionsAsync(
            _options.BotName,
            cancellationToken);

        var longCount = active.Count(x => x.Side == PositionSide.Long);
        var shortCount = active.Count(x => x.Side == PositionSide.Short);

        _logger.LogInformation(
            "BOT8011 active positions. LONG={LongCount}, SHORT={ShortCount}",
            longCount,
            shortCount);

        var oppositePositions = await _positionManager.GetOppositeAsync(
            _options.BotName,
            side,
            cancellationToken);

        foreach (var oppositePosition in oppositePositions)
        {
            await _orders.ClosePositionAsync(
                _options.BotName,
                oppositePosition,
                cancellationToken);

            await _positionManager.MarkClosedAsync(
    oppositePosition,
    cancellationToken);
        }

        var sameSideCount = await _positionManager.CountBySideAsync(
            _options.BotName,
            side,
            cancellationToken);

        if (sameSideCount >= _options.OrderSideLimit)
        {
            _logger.LogInformation(
                "BOT8011 signal blocked. Side={Side}, Count={Count}, Limit={Limit}",
                side,
                sameSideCount,
                _options.OrderSideLimit);

            return false;
        }

        var position = await _orders.OpenBot8011PositionAsync(
            side,
            _options,
            cancellationToken);

        await _positionManager.AddAsync(position, cancellationToken);

        _lastSignalAt[side] = DateTime.UtcNow;

        _logger.LogInformation(
            "BOT8011 signal processed successfully. Side={Side}, PositionId={PositionId}",
            side,
            position.ShortId);

        return true;
    }

    private bool IsInCooldown(PositionSide side)
    {
        if (!_lastSignalAt.TryGetValue(side, out var last))
            return false;

        return DateTime.UtcNow - last < TimeSpan.FromSeconds(_options.CooldownSeconds);
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