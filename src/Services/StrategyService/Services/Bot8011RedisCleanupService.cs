using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders;

namespace StrategyService.Services;

public sealed class Bot8011RedisCleanupService
{
    private readonly Bot8011Options _options;
    private readonly IPositionStore _positionStore;
    private readonly IBinanceFuturesOrderClient _orders;
    private readonly ILogger<Bot8011RedisCleanupService> _logger;

    public Bot8011RedisCleanupService(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient orders,
        ILogger<Bot8011RedisCleanupService> logger)
    {
        _options = options.Value;
        _positionStore = positionStore;
        _orders = orders;
        _logger = logger;
    }

    public async Task<int> CleanupGhostPositionsAsync(CancellationToken cancellationToken)
    {
        var positions = await _positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        var openOrders = await _orders.GetOpenOrdersAsync(
            _options.Symbol,
            cancellationToken);

        var openAlgoOrders = await _orders.GetOpenAlgoOrdersAsync(
            _options.Symbol,
            cancellationToken);

        var cleaned = 0;

        foreach (var position in positions)
        {
            if (position.Closed)
                continue;

            var hasNormalOrder = openOrders.Any(x =>
                IsSame(x.ClientOrderId, position.ParentClientId) ||
                IsSame(x.ClientOrderId, position.TpClientId) ||
                IsSame(x.ClientOrderId, position.CloseClientId));

            var hasAlgoOrder = openAlgoOrders.Any(x =>
                IsSame(x.ClientAlgoId, position.SlClientId) ||
                IsSame(x.ClientAlgoId, position.Stop3ClientId));

            var hasUsefulRedisState =
                position.TpExecuted &&
                position.RemainingQuantity > 0 &&
                position.Stop3Pending &&
                position.ProtectiveActive;

            if (hasNormalOrder || hasAlgoOrder || hasUsefulRedisState)
                continue;

            await _positionStore.DeleteAsync(
                _options.BotName,
                position.ShortId,
                cancellationToken);

            cleaned++;

            _logger.LogWarning(
                "BOT8011 ghost Redis position deleted. Position={ShortId}",
                position.ShortId);
        }

        return cleaned;
    }

    private static bool IsSame(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left) &&
           !string.IsNullOrWhiteSpace(right) &&
           left.Equals(right, StringComparison.OrdinalIgnoreCase);
}