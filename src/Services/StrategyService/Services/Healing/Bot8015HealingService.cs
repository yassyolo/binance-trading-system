using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Services;

public sealed class Bot8015HealingService(
    IOptions<Bot8015Options> options,
    IPositionStore positionStore,
    IBinanceFuturesOrderClient orders,
    IClock clock,
    TelegramNotificationService telegram,
    ILogger<Bot8015HealingService> logger)
    : BackgroundService
{
    private readonly Bot8015Options _options = options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.EnableHealing)
        {
            logger.LogInformation("BOT8015 healing is disabled.");
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
                logger.LogError(
                    ex,
                    "BOT8015 healing cycle failed.");
            }
        }
    }

    private async Task ReconcileAsync(
        CancellationToken cancellationToken)
    {
        var stored = await positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        var candidates = stored
            .Where(x => !x.Closed)
            .Where(x => x.Symbol.Equals(
                _options.Symbol,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (candidates.Length == 0)
            return;

        var normalOrders = await orders.GetOpenOrdersAsync(
            _options.Symbol,
            cancellationToken);

        var algoOrders = await orders.GetOpenAlgoOrdersAsync(
            _options.Symbol,
            cancellationToken);

        var normalClientIds = normalOrders
            .Select(x => x.ClientOrderId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var algoClientIds = algoOrders
            .Select(x => x.ClientAlgoId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var position in candidates)
        {
            var tpPresent =
                !string.IsNullOrWhiteSpace(position.TpClientId)
                && normalClientIds.Contains(position.TpClientId);

            var slPresent =
                !string.IsNullOrWhiteSpace(position.SlClientId)
                && algoClientIds.Contains(position.SlClientId);

            var stop3Present =
                !string.IsNullOrWhiteSpace(position.Stop3ClientId)
                && algoClientIds.Contains(position.Stop3ClientId);

            var normalPhase =
                !position.TpExecuted
                && tpPresent
                && slPresent;

            var stop3Phase =
                position.TpExecuted
                && position.RemainingQuantity > 0
                && stop3Present;

            var transitionPhase =
                position.TpExecuted
                && position.RemainingQuantity > 0
                && position.Stop3Pending;

            if (normalPhase || stop3Phase || transitionPhase)
                continue;

            position.ProtectiveActive = false;
            position.UpdatedAtUtc = clock.UtcNow;

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            await telegram.SendAsync(
                $"🩺 {_options.BotName} reconciliation required\n" +
                $"ID: {position.ShortId}\n" +
                $"TP present: {tpPresent}\n" +
                $"SL present: {slPresent}\n" +
                $"STOP3 present: {stop3Present}\n" +
                $"TP executed: {position.TpExecuted}",
                cancellationToken);

            logger.LogWarning(
                "BOT8015 inconsistent protection state. ShortId={ShortId}, TpPresent={TpPresent}, SlPresent={SlPresent}, Stop3Present={Stop3Present}, TpExecuted={TpExecuted}",
                position.ShortId,
                tpPresent,
                slPresent,
                stop3Present,
                position.TpExecuted);
        }
    }
}
