using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Infrastructure.Locking;
using StrategyService.Market;
using StrategyService.Services;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Market.Contracts;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;

namespace StrategyService.Workers;

public sealed class Bot8011Stop3TrailingWorker(
    IOptions<Bot8011Options> options,
    IPositionStore positionStore,
    IBinanceFuturesMarketClient market,
    SafeBinanceOrderService safeOrders,
    Bot8011Stop3OrderService stop3Orders,
    Bot8011TrailingPriceCache priceCache,
    PositionLockService locks,
    TelegramNotificationService telegram,
    ILogger<Bot8011Stop3TrailingWorker> logger)
    : BackgroundService
{
    private readonly Bot8011Options _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessAsync(stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(_options.TrailingFallbackIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var positions = await positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        foreach (var position in positions.Where(x =>
                     !x.Closed
                     && x.TpExecuted
                     && x.ProtectiveActive
                     && x.Stop3Created
                     && !x.Stop3Pending
                     && !x.TrailingInProgress
                     && x.Stop3Current.HasValue
                     && !string.IsNullOrWhiteSpace(x.Stop3OrderId)
                     && x.RemainingQuantity > 0))
        {
            var currentPrice = priceCache.TryGetFresh(
                TimeSpan.FromSeconds(_options.TrailingPriceMaxAgeSeconds),
                out var klinePrice)
                ? klinePrice
                : await market.GetMarkPriceAsync(
                    position.Symbol,
                    cancellationToken);

            var candidate = Calculate(
                position.Side,
                currentPrice,
                position.Stop3Current.Value);

            if (candidate == position.Stop3Current.Value)
                continue;

            await MoveAsync(
                position.ShortId,
                currentPrice,
                candidate,
                cancellationToken);
        }
    }

    private async Task MoveAsync(
        string shortId,
        decimal currentPrice,
        decimal candidate,
        CancellationToken cancellationToken)
    {
        if (!await locks.TryAcquireAsync(
                _options.BotName,
                shortId,
                TimeSpan.FromSeconds(20)))
            return;

        try
        {
            var position = await positionStore.GetAsync(
                _options.BotName,
                shortId,
                cancellationToken);

            if (position is null
                || position.Closed
                || !position.Stop3Current.HasValue
                || string.IsNullOrWhiteSpace(position.Stop3OrderId))
                return;

            var oldStop = position.Stop3Current.Value;

            position.TrailingInProgress = true;
            position.Stop3NewPending = candidate;
            position.Stop3Previous = oldStop;
            position.UpdatedAtUtc = DateTime.UtcNow;
            await positionStore.SaveAsync(position, cancellationToken);

            var canceled = await safeOrders.SafeCancelAlgoAsync(
                position.Symbol,
                position.Stop3OrderId,
                position.Stop3ClientId,
                cancellationToken);

            if (!canceled)
            {
                position.TrailingInProgress = false;
                position.Stop3NewPending = null;
                await positionStore.SaveAsync(position, cancellationToken);
                return;
            }

            try
            {
                var sequence = position.TrailCount + 1;

                var newStop3 = await stop3Orders.CreateTrailingStop3Async(
                    position,
                    candidate,
                    sequence,
                    cancellationToken);

                position.Stop3ClientId = newStop3.ClientAlgoId;
                position.Stop3OrderId = newStop3.AlgoOrderId;
                position.Stop3Status = newStop3.Status;
                position.Stop3Current = newStop3.TriggerPrice;
                position.Stop3NewPending = null;
                position.TrailingInProgress = false;
                position.TrailCount = sequence;
                position.Status = PositionStatus.Stop3Active;
                position.UpdatedAtUtc = DateTime.UtcNow;

                await positionStore.SaveAsync(position, cancellationToken);

                await telegram.SendAsync(
                    $"🔄 {_options.BotName} STOP3 trailing\n" +
                    $"ID: {position.ShortId}\n" +
                    $"Old: {oldStop}\n" +
                    $"New: {newStop3.TriggerPrice}\n" +
                    $"Current: {currentPrice}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Old STOP3 is canceled and replacement failed.
                position.TrailingInProgress = false;
                position.Stop3Created = false;
                position.Stop3Pending = true;
                position.ProtectiveActive = false;
                position.Status = PositionStatus.Stop3Pending;
                position.UpdatedAtUtc = DateTime.UtcNow;

                await positionStore.SaveAsync(position, cancellationToken);

                logger.LogCritical(
                    ex,
                    "BOT8011 STOP3 replacement failed. Position={ShortId}",
                    position.ShortId);
            }
        }
        finally
        {
            await locks.ReleaseAsync(_options.BotName, shortId);
        }
    }

    private decimal Calculate(
        PositionSide side,
        decimal price,
        decimal currentStop)
    {
        if (side == PositionSide.Long)
            return price >= currentStop + _options.Stop3TrailingStep
                ? currentStop + _options.Stop3TrailingBuffer
                : currentStop;

        return price <= currentStop - _options.Stop3TrailingStep
            ? currentStop - _options.Stop3TrailingBuffer
            : currentStop;
    }
}
