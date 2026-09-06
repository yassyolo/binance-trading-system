using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StrategyService.Bots.Bot8011.Configuration;
using TradingSystem.Application.Locking;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Binance.Market.Contracts;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Bots.Bot8011;

public sealed class Bot8011TrailingWorker(
    IOptions<Bot8011Options> options,
    IPositionStore positionStore,
    IBinanceFuturesMarketClient marketClient,
    SafeBinanceOrderService safe,
    Bot8011Stop3OrderService stop3,
    Bot8011TrailingPriceCache cache,
    IPositionLockProvider locks,
    ILogger<Bot8011TrailingWorker> logger)
    : BackgroundService
{
    private readonly Bot8011Options _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("BOT8011 trailing worker started. Bot = {Bot}, IntervalSeconds = {IntervalSeconds}", _options.BotName, Math.Max(1, _options.TrailingFallbackIntervalSeconds));

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ProcessCycleAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (RedisException redisEx)
                {
                    logger.LogWarning(redisEx, "BOT8011 trailing cycle skipped because Redis is unavailable. The worker will retry.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "BOT8011 trailing cycle failed. The worker will retry.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.TrailingFallbackIntervalSeconds)), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            logger.LogInformation("BOT8011 trailing worker stopped.");
        }
    }

    private async Task ProcessCycleAsync(CancellationToken ct)
    {
        var positions = await positionStore.GetAllAsync(_options.BotName, ct);

        foreach (var position in positions.Where(IsEligibleForTrailing))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var price = cache.TryGetFresh(TimeSpan.FromSeconds(_options.TrailingPriceMaxAgeSeconds), out var cachedPrice)
                    ? cachedPrice
                    : await marketClient.GetMarkPriceAsync(position.Symbol, ct);

                var candidate = ResolveCandidate(position, price);
                if (candidate == position.Stop3Current)
                    continue;

                await MoveAsync(position.ShortId, candidate, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (RedisException redisEx)
            {
                logger.LogWarning(redisEx, "BOT8011 trailing skipped position {ShortId} because Redis is unavailable.", position.ShortId);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BOT8011 trailing failed for position {ShortId}. Remaining positions will still be processed.", position.ShortId);
            }
        }
    }

    private static bool IsEligibleForTrailing(BotPosition position) =>
        !position.Closed &&
        position.TpExecuted &&
        position.Stop3Created &&
        !position.Stop3Pending &&
        !position.TrailingInProgress &&
        position.Stop3Current.HasValue &&
        position.RemainingQuantity > 0;

    private decimal ResolveCandidate(BotPosition position, decimal price)
    {
        var current = position.Stop3Current!.Value;

        if (position.Side == PositionSide.Long && price >= current + _options.Stop3TrailingStep)
            return current + _options.Stop3TrailingBuffer;

        if (position.Side == PositionSide.Short && price <= current - _options.Stop3TrailingStep)
            return current - _options.Stop3TrailingBuffer;

        return current;
    }

    private async Task MoveAsync(string shortId, decimal candidate, CancellationToken ct)
    {
        await using var positionLock = await locks.TryAcquireAsync(_options.BotName, shortId, TimeSpan.FromSeconds(30), ct);

        if (positionLock is null)
            return;

        var position = await positionStore.GetAsync(_options.BotName, shortId, ct);
        if (position is null || position.Closed || string.IsNullOrWhiteSpace(position.Stop3OrderId))
            return;

        position.TrailingInProgress = true;
        position.Stop3NewPending = candidate;
        await positionStore.SaveAsync(position, ct);

        if (!await safe.SafeCancelAlgoAsync(position.Symbol, position.Stop3OrderId, position.Stop3ClientId, ct))
        {
            position.TrailingInProgress = false;
            position.Stop3NewPending = null;
            await positionStore.SaveAsync(position, ct);
            return;
        }

        try
        {
            var sequence = position.TrailCount + 1;
            var newOrder = await stop3.CreateTrailingAsync(position, candidate, sequence, ct);

            position.Stop3ClientId = newOrder.ClientAlgoId;
            position.Stop3OrderId = newOrder.AlgoOrderId;
            position.Stop3Current = newOrder.TriggerPrice;
            position.Stop3Status = newOrder.Status;
            position.Stop3NewPending = null;
            position.TrailingInProgress = false;
            position.TrailCount = sequence;

            await positionStore.SaveAsync(position, ct);
        }
        catch
        {
            position.TrailingInProgress = false;
            position.Stop3NewPending = null;
            position.Stop3Created = false;
            position.Stop3Pending = true;
            position.ProtectiveActive = false;

            try
            {
                await positionStore.SaveAsync(position, CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                logger.LogCritical(rollbackException, "BOT8011 could not persist trailing rollback state. Position = {ShortId}", position.ShortId);
            }

            throw;
        }
    }
}
