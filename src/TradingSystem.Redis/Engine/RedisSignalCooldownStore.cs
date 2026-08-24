using System.Globalization;
using StackExchange.Redis;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Domain.Enums;
using TradingSystem.Redis.Constants;

namespace TradingSystem.Redis.Engine;

public sealed class RedisSignalCooldownStore(
    IConnectionMultiplexer redis,  
    RedisKeyFactory keys,  
    IClock clock) 
    : ISignalCooldownStore
{  
    private readonly IDatabase _db  =  redis.GetDatabase();
    
    public async Task<TimeSpan?> GetRemainingAsync(string botName,  string symbol,  PositionSide side,  DateTime nowUtc,  CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        var key = keys.Cooldown(botName, symbol, side);

        var value = await _db.StringGetAsync(key);
        if (!value.HasValue) 
            return null;
        
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,  out var ms)) 
        { 
            await _db.KeyDeleteAsync(key);
            
            return null;
        }
        
        var remaining = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime - nowUtc;   
        if (remaining > TimeSpan.Zero) 
            return remaining;
        
        await _db.KeyDeleteAsync(key);
        
        return null;
    }
   
    public Task SetAsync(string botName, string symbol, PositionSide side, DateTime expiresAtUtc, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); 
        
        var ttl = expiresAtUtc - clock.UtcNow;    
        if(ttl <= TimeSpan.Zero) 
            return Task.CompletedTask;
       
        var value = new DateTimeOffset(expiresAtUtc).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        
        return _db.StringSetAsync(keys.Cooldown(botName, symbol, side), value, ttl);
    }
}
