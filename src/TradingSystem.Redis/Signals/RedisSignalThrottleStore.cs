using StackExchange.Redis;
using TradingSystem.Signals.Abstractions;

namespace TradingSystem.Redis.Signals;

public sealed class RedisSignalThrottleStore(
	IConnectionMultiplexer redis)
	: IDistributedSignalThrottleStore
{
	public Task<bool> TryAcquireAsync(string bot, string symbol, string side, DateTime at, TimeSpan interval, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		
		if(interval <= TimeSpan.Zero)
			return Task.FromResult(true);	
		
		var key = $"trading:signal-throttle:{Normalize(bot)}:{Normalize(symbol)}:{Normalize(side)}";
		
		return redis.GetDatabase().StringSetAsync(key, new DateTimeOffset(at).ToUnixTimeMilliseconds(), interval, When.NotExists);
	}
	
	static string Normalize(string x)
		=> x.Trim().ToUpperInvariant();
}