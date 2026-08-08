using StackExchange.Redis; 
using TradingSystem.Application.Events;

namespace TradingSystem.Redis.Events;

public sealed class RedisEventDeduplicationStore(
    IConnectionMultiplexer redis)
    :IEventDeduplicationStore
{
    private readonly IDatabase _db = redis.GetDatabase();
    
    public Task<bool> TryBeginAsync(string eventKey, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        return _db.StringSetAsync($"trading:event-dedup:{eventKey}", "1", ttl, When.NotExists);
    }
}