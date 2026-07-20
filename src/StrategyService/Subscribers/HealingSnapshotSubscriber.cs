using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TradingSystem.Application.Healing;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.UserStream;
using TradingSystem.Infrastructure.Serialization;

namespace StrategyService.Subscribers;

public sealed class HealingSnapshotSubscriber(
    IConnectionMultiplexer redis,
    HealingServiceRegistry registry,
    ILogger<HealingSnapshotSubscriber> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var sub = redis.GetSubscriber();
        await sub.SubscribeAsync(
            RedisChannel.Literal(RedisChannels.Healing),
            async (_, m) =>
            {
                if (!m.HasValue)
                    return;
                try
                {
                    var s = JsonSerializer.Deserialize<HealingSnapshotMessage>(
                        m.ToString(),
                        JsonDefaults.Messaging // Use the correct options property
                    );
                    if (s is null)
                        return;
                    foreach (var service in registry.ForSymbol(s.Symbol))
                        await service.HealAsync(s, token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Healing snapshot processing failed.");
                }
            }
        );
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            await sub.UnsubscribeAsync(RedisChannel.Literal(RedisChannels.Healing));
        }
    }
}
