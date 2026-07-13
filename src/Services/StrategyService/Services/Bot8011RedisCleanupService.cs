using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Services;

public sealed class Bot8011RedisCleanupService(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient orders,
        ILogger<Bot8011RedisCleanupService> logger)
{
    private readonly Bot8011Options options = options.Value;
    public async Task<int> CleanupGhostPositionsAsync(CancellationToken cancellationToken)
    {
        var positions = await positionStore.GetAllAsync(
            options.BotName,
            cancellationToken);

        var openOrders = await orders.GetOpenOrdersAsync(
            options.Symbol,
            cancellationToken);

        var openAlgoOrders = await orders.GetOpenAlgoOrdersAsync(
            options.Symbol,
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

            await positionStore.DeleteAsync(
                options.BotName,
                position.ShortId,
                cancellationToken);

            cleaned++;

            logger.LogWarning(
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