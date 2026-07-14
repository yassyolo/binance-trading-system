using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TradingSystem.Application.Execution;
using TradingSystem.Contracts.Redis;
using TradingSystem.Domain.Signals;
using TradingSystem.Infrastructure.Serialization;

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
        var channel = RedisChannel.Literal(RedisChannels.StrategySignals);

        await subscriber.SubscribeAsync(channel, async (_, message) =>
        {
            if (!message.HasValue || stoppingToken.IsCancellationRequested)
                return;

            await ProcessAsync(message.ToString(), stoppingToken);
        });

        logger.LogInformation("Subscribed to strategy signals. Channel={Channel}", channel);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await subscriber.UnsubscribeAsync(channel);
        }
    }

    private async Task ProcessAsync(string raw, CancellationToken cancellationToken)
    {
        try
        {
            var signal = JsonSerializer.Deserialize<TradeSignal>(raw, JsonDefaults.CaseInsensitive);
            if (signal is not null)
                await signalHandler.HandleAsync(signal, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trading signal processing failed. Raw={Raw}", raw);
        }
    }
}
