using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Models;
using StrategyService.Services;

namespace StrategyService.Strategies;

public sealed class Bot8011Strategy
{
    private readonly Bot8011Options _options;
    private readonly PositionManager _positionManager;
    private readonly OrderExecutionService _orders;
    private readonly ILogger<Bot8011Strategy> _logger;

    private readonly Dictionary<string, DateTime> _lastSignalAt = new();

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
        var side = signal.Action.ToUpperInvariant();

        if (side is not "LONG" and not "SHORT")
        {
            _logger.LogWarning("Invalid BOT8011 signal action: {Action}", signal.Action);
            return false;
        }

        if (side == "LONG" && !_options.EnableLong)
            return false;

        if (side == "SHORT" && !_options.EnableShort)
            return false;

        if (IsInCooldown(side))
        {
            _logger.LogInformation("BOT8011 {Side} signal ignored because of cooldown.", side);
            return false;
        }

        var active = _positionManager.GetActivePositions();

        var longCount = active.Count(x => x.Side == "LONG");
        var shortCount = active.Count(x => x.Side == "SHORT");

        _logger.LogInformation(
            "BOT8011 active positions. LONG={LongCount}, SHORT={ShortCount}",
            longCount,
            shortCount);

        _positionManager.CloseOpposite(side);

        var sameSideCount = _positionManager.CountBySide(side);

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

        _positionManager.Add(position);
        _lastSignalAt[side] = DateTime.UtcNow;

        _logger.LogInformation(
            "BOT8011 signal processed successfully. Side={Side}, PositionId={PositionId}",
            side,
            position.PositionId);

        return true;
    }

    private bool IsInCooldown(string side)
    {
        if (!_lastSignalAt.TryGetValue(side, out var last))
            return false;

        return DateTime.UtcNow - last < TimeSpan.FromSeconds(_options.CooldownSeconds);
    }
}