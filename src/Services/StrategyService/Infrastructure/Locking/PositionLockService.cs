using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;
using TradingSystem.Redis.Configuration;

namespace StrategyService.Infrastructure.Locking;

public sealed class PositionLockService(
        IConnectionMultiplexer redis,
        IOptions<RedisPositionStoreOptions> options)
{
    private readonly IDatabase database = redis.GetDatabase();
    private readonly RedisPositionStoreOptions options = options.Value;

    public async Task<bool> TryAcquireAsync(
        string botName,
        string shortId,
        TimeSpan ttl)
    {
        var key = GetLockKey(botName, shortId);

        return await database.StringSetAsync(
            key,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ttl,
            When.NotExists);
    }

    public Task ReleaseAsync(string botName, string shortId)
    {
        var key = GetLockKey(botName, shortId);

        return database.KeyDeleteAsync(key);
    }

    private RedisKey GetLockKey(string botName, string shortId)
    {
        var key = RedisKeys.PositionLock(botName, shortId);

        return string.IsNullOrWhiteSpace(options.Prefix)
            ? key
            : $"{options.Prefix}:{key}";
    }
}