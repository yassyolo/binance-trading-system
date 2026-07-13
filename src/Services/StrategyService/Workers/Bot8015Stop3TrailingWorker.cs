using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Infrastructure.Locking;
using StrategyService.Market;
using StrategyService.Services;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Market.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Workers;

public sealed class Bot8015Stop3TrailingWorker(
    IOptions<Bot8015Options> options,
    IPositionStore positionStore,
    Bot8015Stop3OrderService stop3Orders,
    Bot8015TrailingPriceCache priceCache,
    IBinanceFuturesMarketClient market,
    PositionLockService locks,
    IClock clock,
    TelegramNotificationService telegram,
    ILogger<Bot8015Stop3TrailingWorker> logger)
    : BackgroundService
{
    private readonly Bot8015Options _options = options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.EnableTrailing)
        {
            logger.LogInformation("BOT8015 trailing is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.TrailingCheckIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(stoppingToken);
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
                    "BOT8015 trailing cycle failed.");
            }
        }
    }

    private async Task ProcessAsync(
        CancellationToken cancellationToken)
    {
        var price = await ResolveTrailingPriceAsync(
            cancellationToken);

        if (price <= 0)
            return;

        var positions = await positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        foreach (var position in positions.Where(ShouldTrail))
        {
            await TryTrailAsync(
                position,
                price,
                cancellationToken);
        }
    }

    private async Task<decimal> ResolveTrailingPriceAsync(
        CancellationToken cancellationToken)
    {
        if (priceCache.TryGetFresh(
                TimeSpan.FromMinutes(2),
                out var klineClose))
        {
            return klineClose;
        }

        return await market.GetMarkPriceAsync(
            _options.Symbol,
            cancellationToken);
    }

    private bool ShouldTrail(BotPosition position)
        => !position.Closed
           && position.Symbol.Equals(
               _options.Symbol,
               StringComparison.OrdinalIgnoreCase)
           && position.TpExecuted
           && position.ProtectiveActive
           && position.Stop3Created
           && !position.Stop3Pending
           && !position.TrailingInProgress
           && position.RemainingQuantity > 0
           && position.Stop3Current is > 0;

    private async Task TryTrailAsync(
        BotPosition position,
        decimal currentPrice,
        CancellationToken cancellationToken)
    {
        var currentStop = position.Stop3Current!.Value;

        var shouldMove = position.Side switch
        {
            PositionSide.Long =>
                currentPrice >= currentStop + _options.Stop3TrailingStep,

            PositionSide.Short =>
                currentPrice <= currentStop - _options.Stop3TrailingStep,

            _ => false
        };

        if (!shouldMove)
            return;

        var newStop = position.Side switch
        {
            PositionSide.Long =>
                currentStop + _options.Stop3TrailingBuffer,

            PositionSide.Short =>
                currentStop - _options.Stop3TrailingBuffer,

            _ => currentStop
        };

        if (!IsImproved(position.Side, currentStop, newStop))
            return;

        var acquired = await locks.TryAcquireAsync(
            _options.BotName,
            position.ShortId,
            TimeSpan.FromSeconds(20));

        if (!acquired)
            return;

        try
        {
            var latest = await positionStore.GetAsync(
                _options.BotName,
                position.ShortId,
                cancellationToken);

            if (latest is null || !ShouldTrail(latest))
                return;

            latest.TrailingInProgress = true;
            latest.Stop3NewPending = newStop;
            latest.Stop3Previous = latest.Stop3Current;
            latest.UpdatedAtUtc = clock.UtcNow;

            await positionStore.SaveAsync(
                latest,
                cancellationToken);

            try
            {
                await stop3Orders.ReplaceAsync(
                    latest,
                    newStop,
                    cancellationToken);

                latest.TrailingInProgress = false;
                latest.Stop3NewPending = null;
                latest.UpdatedAtUtc = clock.UtcNow;

                await positionStore.SaveAsync(
                    latest,
                    cancellationToken);

                await telegram.SendAsync(
                    $"🔄 {_options.BotName} STOP3 Trailing\n" +
                    $"ID: {latest.ShortId}\n" +
                    $"Side: {latest.Side}\n" +
                    $"Old: {currentStop}\n" +
                    $"New: {newStop}\n" +
                    $"Price: {currentPrice}",
                    cancellationToken);
            }
            catch
            {
                latest.TrailingInProgress = false;
                latest.Stop3NewPending = null;
                latest.UpdatedAtUtc = clock.UtcNow;

                await positionStore.SaveAsync(
                    latest,
                    cancellationToken);

                throw;
            }
        }
        finally
        {
            await locks.ReleaseAsync(
                _options.BotName,
                position.ShortId);
        }
    }

    private static bool IsImproved(
        PositionSide side,
        decimal currentStop,
        decimal newStop)
        => side switch
        {
            PositionSide.Long => newStop > currentStop,
            PositionSide.Short => newStop < currentStop,
            _ => false
        };
}
