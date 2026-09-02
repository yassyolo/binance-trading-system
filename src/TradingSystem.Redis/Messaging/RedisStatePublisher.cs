using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Infrastructure.Serialization;
using TradingSystem.Redis.Messaging.Contracts;

namespace TradingSystem.Redis.Messaging;

public sealed class RedisStatePublisher(
    IConnectionMultiplexer redis) 
    : IRedisStatePublisher
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly ISubscriber _subscriber = redis.GetSubscriber();
    
    public async Task SetAndPublishAsync(string key, string channel, object payload, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        var json = JsonSerializer.Serialize(payload,  JsonDefaults.Messaging);
        
        await _database.StringSetAsync(key,  json);
        await _subscriber.PublishAsync(RedisChannel.Literal(channel),  json);
    }
}
