using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Linq;
using TradingSystem.Application.Locking;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Market.Contracts;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Bots.Bot8015;

public sealed class Bot8015TrailingWorker(
    IOptions<Bot8015Options> options,
    IPositionStore store,
    IBinanceFuturesMarketClient market,
    SafeBinanceOrderService safe,
    Bot8015Stop3OrderService stop3,
    Bot8015TrailingPriceCache cache,
    IPositionLockProvider locks,
    ILogger<Bot8015TrailingWorker> logger)
    : BackgroundService
{
    private readonly Bot8015Options _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException exception)
            {
                logger.LogWarning(
                    exception,
                    "BOT8015 trailing cycle failed because Redis is unavailable. The worker will retry.");
            }
            catch (Exception exception)
            {
                // A failed cycle must not terminate StrategyService.
                logger.LogError(
                    exception,
                    "BOT8015 trailing cycle failed. The worker will retry.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Max(1, _options.TrailingCheckIntervalSeconds)),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessCycleAsync(CancellationToken ct)
    {
        var positions = await store.GetAllAsync(_options.BotName, ct);

        foreach (var position in positions.Where(IsEligibleForTrailing))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var price = cache.TryGetFresh(
                    TimeSpan.FromSeconds(_options.TrailingPriceMaxAgeSeconds),
                    out var cachedPrice)
                        ? cachedPrice
                        : await market.GetMarkPriceAsync(position.Symbol, ct);

                var candidate = ResolveCandidate(position, price);
                if (candidate == position.Stop3Current)
                    continue;

                await MoveAsync(position.ShortId, candidate, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // One bad position must not block trailing for the remaining positions.
                logger.LogError(
                    exception,
                    "BOT8015 trailing failed for position {ShortId}. Processing will continue.",
                    position.ShortId);
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

        if (position.Side == PositionSide.Long &&
            price >= current + _options.Stop3TrailingStep)
        {
            return current + _options.Stop3TrailingBuffer;
        }

        if (position.Side == PositionSide.Short &&
            price <= current - _options.Stop3TrailingStep)
        {
            return current - _options.Stop3TrailingBuffer;
        }

        return current;
    }

    private async Task MoveAsync(string shortId, decimal candidate, CancellationToken ct)
    {
        await using var positionLock = await locks.TryAcquireAsync(
            _options.BotName,
            shortId,
            TimeSpan.FromSeconds(30),
            ct);

        if (positionLock is null)
            return;

        var position = await store.GetAsync(_options.BotName, shortId, ct);
        if (position is null || position.Closed || string.IsNullOrWhiteSpace(position.Stop3OrderId))
            return;

        position.TrailingInProgress = true;
        position.Stop3NewPending = candidate;
        await store.SaveAsync(position, ct);

        if (!await safe.SafeCancelAlgoAsync(
                position.Symbol,
                position.Stop3OrderId,
                position.Stop3ClientId,
                ct))
        {
            position.TrailingInProgress = false;
            position.Stop3NewPending = null;
            await store.SaveAsync(position, ct);
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

            await store.SaveAsync(position, ct);
        }
        catch
        {
            position.TrailingInProgress = false;
            position.Stop3NewPending = null;
            position.Stop3Created = false;
            position.Stop3Pending = true;
            position.ProtectiveActive = false;

            await store.SaveAsync(position, CancellationToken.None);
            throw;
        }
    }
}
