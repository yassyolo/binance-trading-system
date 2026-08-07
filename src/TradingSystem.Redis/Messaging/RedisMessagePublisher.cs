using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Infrastructure.Serialization;
using TradingSystem.Redis.Messaging.Contracts;

namespace TradingSystem.Redis.Messaging;

public sealed class RedisMessagePublisher(
    IConnectionMultiplexer redis) 
    : IRedisMessagePublisher
{
    private readonly ISubscriber _subscriber = redis.GetSubscriber();
    
    public async Task PublishAsync<T>(string channel, T message, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        var json = JsonSerializer.Serialize(message, JsonDefaults.Messaging);
        
        await _subscriber.PublishAsync(RedisChannel.Literal(channel), json);
    }
}
