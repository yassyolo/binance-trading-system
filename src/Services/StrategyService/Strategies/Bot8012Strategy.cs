using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Models;
using StrategyService.Services;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies;

public sealed class Bot8012Strategy
{
    private const string BotName = "BOT8012";

    private readonly Bot8012Options _options;
    private readonly PositionManager _positions;
    private readonly OrderExecutionService _orders;
    private readonly ILogger<Bot8012Strategy> _logger;

    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = new();

    public Bot8012Strategy(
        IOptions<Bot8012Options> options,
        PositionManager positions,
        OrderExecutionService orders,
        ILogger<Bot8012Strategy> logger)
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

        if (side == PositionSide.Long && !_options.EnableLong)
            return false;

        if (side == PositionSide.Short && !_options.EnableShort)
            return false;

        if (IsInCooldown(side))
        {
            _logger.LogInformation("{BotName} {Side} signal ignored because of cooldown.", BotName, side);
            return false;
        }

        var sameSideCount = await _positions.CountBySideAsync(BotName, side, cancellationToken);

        if (sameSideCount >= _options.OrderSideLimit)
        {
            _logger.LogInformation(
                "{BotName} blocked. Side={Side}, Count={Count}, Limit={Limit}",
                BotName,
                side,
                sameSideCount,
                _options.OrderSideLimit);

            return false;
        }

        var position = await _orders.OpenTpOnlyPositionAsync(
            botName: BotName,
            side: side,
            symbol: _options.Symbol,
            quantity: _options.Quantity,
            profitDistance: _options.ProfitDistance,
            cancellationToken: cancellationToken);

        await _positions.AddAsync(position, cancellationToken);

        _lastSignalAt[side] = DateTime.UtcNow;

        _logger.LogInformation(
            "{BotName} signal processed. Side={Side}, PositionId={PositionId}",
            BotName,
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