using System.Text.Json;
using StackExchange.Redis;

namespace StrategyService.Services;

public sealed class HealingSnapshotSubscriber : BackgroundService
{
    private const string HealingChannel = "binance:system:healing";

    private readonly IConnectionMultiplexer _redis;
    private readonly Bot8011HealingService _healingService;
    private readonly ILogger<HealingSnapshotSubscriber> _logger;

    public HealingSnapshotSubscriber(
        IConnectionMultiplexer redis,
        Bot8011HealingService healingService,
        ILogger<HealingSnapshotSubscriber> logger)
    {
        _redis = redis;
        _healingService = healingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(HealingChannel),
            async (_, message) =>
            {
                if (!message.HasValue)
                    return;

                try
                {
                    var snapshot = JsonSerializer.Deserialize<HealingSnapshotDto>(
                        message.ToString());

                    if (snapshot is null)
                        return;

                    await _healingService.HealAsync(
                        snapshot,
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "BOT8011 healing snapshot processing failed.");
                }
            });

        _logger.LogInformation(
            "BOT8011 subscribed to healing channel {Channel}",
            HealingChannel);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}