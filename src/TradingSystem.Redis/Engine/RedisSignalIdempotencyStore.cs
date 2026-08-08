using StackExchange.Redis;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Redis.Constants;

namespace TradingSystem.Redis.Engine;

public sealed class RedisSignalIdempotencyStore(
    IConnectionMultiplexer redis, 
    RedisKeyFactory keys) 
    : ISignalIdempotencyStore
{
    private readonly IDatabase _db = redis.GetDatabase();
    
    public Task<bool> TryStartAsync(string id, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        return _db.StringSetAsync(keys.SignalIdempotency(id), "processing", ttl, When.NotExists);
    }
    
    public Task MarkCompletedAsync(string id, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        return _db.StringSetAsync(keys.SignalIdempotency(id), "completed", ttl);
    }
    
    public Task ReleaseAsync(string id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        return _db.KeyDeleteAsync(keys.SignalIdempotency(id));
    }
}
