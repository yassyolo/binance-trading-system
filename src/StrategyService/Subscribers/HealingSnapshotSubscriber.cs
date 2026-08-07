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

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var channel = RedisChannel.Literal(RedisChannels.Healing);

        while (!token.IsCancellationRequested)
        {
            var subscriber = redis.GetSubscriber();
            var subscribed = false;

            try
            {
                await subscriber.SubscribeAsync(channel, async (_, message) =>
                {
                    if (!message.HasValue || token.IsCancellationRequested)
                        return;

                    await ProcessSafelyAsync(message.ToString(), token);
                });

                subscribed = true;
                
                logger.LogInformation("Subscribed to healing snapshots. Channel = {Channel}", channel);
                
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException exception)
            {
                logger.LogWarning(exception, "Could not subscribe to healing snapshots because Redis is unavailable. Channel = {Channel}. Retrying.", channel);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Healing snapshot subscription failed. Channel = {Channel}. Retrying.", channel);
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

            await DelayBeforeRetryAsync(token);
        }
    }

    private async Task ProcessSafelyAsync(string raw, CancellationToken token)
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
                token.ThrowIfCancellationRequested();
                await service.HealAsync(snapshot, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Invalid healing snapshot JSON. Payload = {Payload}", raw);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Healing snapshot processing failed.");
        }
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
