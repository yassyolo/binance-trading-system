using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Time;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.Signals;
using TradingSystem.Infrastructure.Serialization;

namespace TradingSystem.Redis.Signals;

public sealed class RedisTradingSignalSubscriber(IConnectionMultiplexer redis, ITradingSignalHandler handler, IClock clock, ILogger<RedisTradingSignalSubscriber> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();
        var channel = RedisChannel.Literal(RedisChannels.StrategySignals);
        await subscriber.SubscribeAsync(channel, async (_, value) =>
        {
            if (value.HasValue && !stoppingToken.IsCancellationRequested)
                await ProcessSafelyAsync(value.ToString(), stoppingToken);
        });
        logger.LogInformation("Subscribed to trading signals. Channel = {Channel}", channel);
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            await subscriber.UnsubscribeAsync(channel);
        }
    }
    private async Task ProcessSafelyAsync(string raw, CancellationToken ct)
    {
        try{var message = JsonSerializer.Deserialize<TradingSignalMessage>(raw, JsonDefaults.Messaging);if(message is null)return;await handler.HandleAsync(TradingSignalMessageMapper.Map(message, clock.UtcNow), ct);}
        catch(OperationCanceledException)when(ct.IsCancellationRequested){}
        catch(Exception ex){logger.LogError(ex, "Trading signal processing failed. Raw = {Raw}", raw);}
    }
}
