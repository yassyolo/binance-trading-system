using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Application.Healing;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.UserStream;
using TradingSystem.Infrastructure.Serialization;

namespace StrategyService.Subscribers;

public sealed class HealingSnapshotSubscriber(
    IConnectionMultiplexer redis,
    HealingServiceRegistry registry,
    ILogger<HealingSnapshotSubscriber> logger) 
    : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var channel = RedisChannel.Literal(RedisChannels.Healing);

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
                
                logger.LogInformation("Subscribed to healing snapshots. Channel = {Channel}", channel);
                
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException redisEx)
            {
                logger.LogWarning(redisEx, "Could not subscribe to healing snapshots because Redis is unavailable. Channel = {Channel}. Retrying.", channel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Healing snapshot subscription failed. Channel = {Channel}. Retrying.", channel);
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
                        logger.LogWarning(exception, "Could not unsubscribe cleanly from healing channel {Channel}", channel);
                    }
                }
            }

            await DelayBeforeRetryAsync(ct);
        }
    }

    private async Task ProcessSafelyAsync(string raw, CancellationToken ct)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<HealingSnapshotMessage>(raw, JsonDefaults.Messaging);
            if (snapshot is null)
            {
                logger.LogWarning("Healing snapshot deserialization returned null.");
                return;
            }

            foreach (var service in registry.ForSymbol(snapshot.Symbol))
            {
                ct.ThrowIfCancellationRequested();
                
                await service.HealAsync(snapshot, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {}
        catch (JsonException jsonEx)
        {
            logger.LogWarning(jsonEx, "Invalid healing snapshot JSON. Payload = {Payload}", raw);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Healing snapshot processing failed.");
        }
    }

    private static async Task DelayBeforeRetryAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(RetryDelay, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {}
    }
}
