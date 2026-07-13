using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Services;

public sealed class Bot8014HealingService(
    IOptions<Bot8014Options> options,
    IPositionStore positionStore,
    IBinanceFuturesOrderClient orders,
    IClock clock,
    TelegramNotificationService telegram,
    ILogger<Bot8014HealingService> logger)
    : BackgroundService
{
    private readonly Bot8014Options _options = options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.EnableHealing)
        {
            logger.LogInformation("BOT8014 healing is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.HealingIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BOT8014 healing cycle failed.");
            }
        }
    }

    private async Task ReconcileAsync(
        CancellationToken cancellationToken)
    {
        var storedPositions = await positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        var candidates = storedPositions
            .Where(x => !x.Closed)
            .Where(x => x.Symbol.Equals(
                _options.Symbol,
                StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.IsNullOrWhiteSpace(x.TpClientId))
            .ToArray();

        if (candidates.Length == 0)
            return;

        var openOrders = await orders.GetOpenOrdersAsync(
            _options.Symbol,
            cancellationToken);

        var activeClientIds = openOrders
            .Select(x => x.ClientOrderId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var position in candidates)
        {
            if (activeClientIds.Contains(position.TpClientId!))
                continue;

            if (!position.ProtectiveActive &&
                position.TpStatus?.Equals(
                    "MISSING_REQUIRES_RECONCILIATION",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            position.TpStatus = "MISSING_REQUIRES_RECONCILIATION";
            position.ProtectiveActive = false;
            position.UpdatedAtUtc = clock.UtcNow;

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            await telegram.SendAsync(
                $"🩺 {_options.BotName} TP_MISSING\n" +
                $"Side: {position.Side}\n" +
                $"ShortId: {position.ShortId}\n" +
                $"TP ClientId: {position.TpClientId}\n" +
                $"Position was preserved and requires Binance reconciliation.",
                cancellationToken);

            logger.LogWarning(
                "BOT8014 TP missing from Binance open orders. Position preserved. ShortId={ShortId}, TpClientId={TpClientId}",
                position.ShortId,
                position.TpClientId);
        }
    }
}
