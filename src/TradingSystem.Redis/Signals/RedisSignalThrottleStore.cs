using StackExchange.Redis;
using TradingSystem.Signals.Abstractions;

namespace TradingSystem.Redis.Signals;

public sealed class RedisSignalThrottleStore(
	IConnectionMultiplexer redis)
	:IDistributedSignalThrottleStore
{
	public Task<bool> TryAcquireAsync(string bot, string symbol, string side, DateTime at, TimeSpan interval, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		
		if(interval <= TimeSpan.Zero)
			return Task.FromResult(true);	
		
		var key = $"trading:signal-throttle:{N(bot)}:{N(symbol)}:{N(side)}";
		
		return redis.GetDatabase()
			.StringSetAsync(key, new DateTimeOffset(at).ToUnixTimeMilliseconds(), interval, When.NotExists);
	}
	
	static string N(string x) => x.Trim().ToUpperInvariant();
}