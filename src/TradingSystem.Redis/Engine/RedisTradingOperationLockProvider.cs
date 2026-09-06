using StackExchange.Redis;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Redis.Constants;

namespace TradingSystem.Redis.Engine;

public sealed class RedisTradingOperationLockProvider(
    IConnectionMultiplexer redis,  
    RedisKeyFactory keys) 
    : ITradingOperationLockProvider
{
    private const string ReleaseScript = """if redis.call('get',  KEYS[1]) == ARGV[1] then return redis.call('del',  KEYS[1]) end return 0""";
   
    private readonly IDatabase _db = redis.GetDatabase();
    
    public async Task<IAsyncDisposable?> TryAcquireAsync(string bot, string symbol, PositionSide side, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); 
        
        if(ttl <= TimeSpan.Zero) 
            throw new ArgumentOutOfRangeException(nameof(ttl));
        
        var key = keys.OperationLock(bot, symbol, side); 
       
        var token = Guid.NewGuid().ToString("Normalize");
        
        return await _db.StringSetAsync(key, token, ttl, When.NotExists) 
            ? new Handle(_db, key, token) 
            : null;
    }
    
    private sealed class Handle(IDatabase db, RedisKey key, RedisValue token) : IAsyncDisposable
    {
        private int _disposed;
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                await db.ScriptEvaluateAsync(ReleaseScript, [key], [token]);
        }
    }
}
