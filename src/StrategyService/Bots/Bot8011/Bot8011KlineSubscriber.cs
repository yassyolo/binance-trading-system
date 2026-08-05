using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Messaging;

namespace StrategyService.Bots.Bot8011;

public sealed class Bot8011KlineSubscriber(
    IConnectionMultiplexer redis,
    IOptions<Bot8011Options> options,
    Bot8011TrailingPriceCache cache,
    ILogger<Bot8011KlineSubscriber> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var channel = RedisChannel.Literal(RedisChannels.Kline(settings.TrailingInterval, settings.Symbol));

        while (!stoppingToken.IsCancellationRequested)
        {
            var subscriber = redis.GetSubscriber();
            var subscribed = false;

            try
            {
                await subscriber.SubscribeAsync(channel, async (_, message) =>
                {
                    if (!message.HasValue || stoppingToken.IsCancellationRequested)
                        return;

                    await ProcessSafelyAsync(message.ToString(), stoppingToken);
                });

                subscribed = true;
                logger.LogInformation("BOT8011 subscribed to trailing-price channel {Channel}", channel);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException exception)
            {
                logger.LogWarning(
                    exception,
                    "BOT8011 could not subscribe because Redis is unavailable. Channel = {Channel}. Retrying.",
                    channel);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "BOT8011 trailing-price subscription failed. Channel = {Channel}. Retrying.", channel);
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
                        logger.LogWarning(exception, "BOT8011 could not unsubscribe cleanly from {Channel}", channel);
                    }
                }
            }

            await DelayBeforeRetryAsync(stoppingToken);
        }

        logger.LogInformation("BOT8011 kline subscriber stopped. Channel = {Channel}", channel);
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
                logger.LogDebug("BOT8011 kline payload has no close price.");
                return Task.CompletedTask;
            }

            if (!decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                logger.LogWarning("BOT8011 ignored invalid trailing price. Value = {Value}", value.ToString());
                return Task.CompletedTask;
            }

            cache.Set(price);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "BOT8011 received invalid kline JSON. Payload = {Payload}", raw);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "BOT8011 kline message processing failed.");
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
