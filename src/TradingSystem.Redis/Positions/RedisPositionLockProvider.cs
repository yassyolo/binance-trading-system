using StackExchange.Redis;
using TradingSystem.Application.Locking;

namespace TradingSystem.Redis.Positions;

public sealed class RedisPositionLockProvider(
    IConnectionMultiplexer redis)
    :IPositionLockProvider
{
    const string Script = "if redis.call('get', KEYS[1])==ARGV[1] then return redis.call('del', KEYS[1]) end return 0";
    public async Task<IAsyncDisposable?> TryAcquireAsync(string bot, string id, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var db = redis.GetDatabase();
        var key = $"trading:position-lock:{bot.Trim().ToUpperInvariant()}:{id}";
        var token = Guid.NewGuid().ToString("N");
        return await db.StringSetAsync(key, token, ttl, When.NotExists)?new H(db, key, token):null;
    }
    
    sealed class H(IDatabase db, RedisKey key, RedisValue token)
        :IAsyncDisposable
    {
        int d;
        public async ValueTask DisposeAsync()
        {
            if(Interlocked.Exchange(ref d, 1)==0)
                await db.ScriptEvaluateAsync(Script, [key], [token]);
        }
    }
}
