using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Messaging;

namespace StrategyService.Bots.Bot8015;

public sealed class Bot8015KlineSubscriber(
    IConnectionMultiplexer redis,
    IOptions<Bot8015Options> options,
    Bot8015TrailingPriceCache cache,
    ILogger<Bot8015KlineSubscriber> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var settings = options.Value;
        var channel = RedisChannel.Literal(RedisChannels.Kline(settings.KlineInterval, settings.Symbol));

        while (!ct.IsCancellationRequested)
        {
            var subscriber = redis.GetSubscriber();
            var subscribed = false;

            try
            {
                await subscriber.SubscribeAsync(channel, async (_, message) =>
                {
                    if (!message.HasValue || ct.IsCancellationRequested)
                        return;

                    await ProcessSafelyAsync(message.ToString(), ct);
                });

                subscribed = true;
                logger.LogInformation("BOT8015 subscribed to trailing-price channel {Channel}", channel);
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException exception)
            {
                logger.LogWarning(
                    exception,
                    "BOT8015 could not subscribe because Redis is unavailable. Channel = {Channel}. Retrying.",
                    channel);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "BOT8015 trailing-price subscription failed. Channel = {Channel}. Retrying.", channel);
            }
            finally
            {
                if (subscribed)
                {
                    try
                    {
                        await subscriber.UnsubscribeAsync(channel);
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, "BOT8015 could not unsubscribe cleanly from {Channel}", channel);
                    }
                }
            }

            await DelayBeforeRetryAsync(ct);
        }
    }

    private Task ProcessSafelyAsync(string raw, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            if (!(root.TryGetProperty("close", out var value) || root.TryGetProperty("c", out value)))
            {
                logger.LogDebug("BOT8015 kline payload has no close price.");
                return Task.CompletedTask;
            }

            if (!decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                logger.LogWarning("BOT8015 ignored invalid trailing price. Value = {Value}", value.ToString());
                return Task.CompletedTask;
            }

            cache.Set(price);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "BOT8015 received invalid kline JSON. Payload = {Payload}", raw);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "BOT8015 kline message processing failed.");
        }

        return Task.CompletedTask;
    }

    private static async Task DelayBeforeRetryAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(RetryDelay, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }
}
