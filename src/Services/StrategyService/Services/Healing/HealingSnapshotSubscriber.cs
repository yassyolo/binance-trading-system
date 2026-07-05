using System.Text.Json;
using StackExchange.Redis;
using StrategyService.Services.Healing;
using TradingSystem.Domain.Healing;

namespace StrategyService.Services;

public sealed class HealingSnapshotSubscriber(
    IConnectionMultiplexer redis,
    IEnumerable<IBotHealingService> healingServices,
    ILogger<HealingSnapshotSubscriber> logger) : BackgroundService
{
    private const string HealingChannel = "binance:system:healing";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(HealingChannel),
            async (_, message) =>
            {
                if (!message.HasValue)
                    return;

                try
                {
                    var snapshot = JsonSerializer.Deserialize<HealingSnapshot>(
                        message.ToString());

                    if (snapshot is null)
                        return;

                    await Task.WhenAll(
    healingServices.Select(x =>
        x.HealAsync(snapshot, stoppingToken)));
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Healing snapshot processing failed.");
                }
            });

        logger.LogInformation("Subscribed to healing channel {Channel}", HealingChannel);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}