using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TradingSystem.Application.Execution;
using TradingSystem.Contracts.Redis;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Redis.Subscribers;

public sealed class RedisSignalSubscriber(
        IConnectionMultiplexer redis,
        ITradingSignalHandler signalHandler,
        ILogger<RedisSignalSubscriber> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(RedisChannels.StrategySignals),
            async (_, message) =>
            {
                if (!message.HasValue)
                    return;

                try
                {
                    var signal = JsonSerializer.Deserialize<TradingSignal>(
                        message.ToString(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (signal is null)
                        return;

                    await signalHandler.HandleAsync(signal, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Trading signal processing failed. Raw={Raw}", message.ToString());
                }
            });

        logger.LogInformation(
            "Subscribed to strategy signal channel {Channel}",
            RedisChannels.StrategySignals);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}