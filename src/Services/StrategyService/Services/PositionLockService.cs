using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;
using TradingSystem.Redis.Configuration;

namespace StrategyService.Services;

public sealed class PositionLockService
{
    private readonly IDatabase _database;
    private readonly RedisPositionStoreOptions _options;

    public PositionLockService(
        IConnectionMultiplexer redis,
        IOptions<RedisPositionStoreOptions> options)
    {
        _database = redis.GetDatabase();
        _options = options.Value;
    }

    public async Task<bool> TryAcquireAsync(
        string botName,
        string shortId,
        TimeSpan ttl)
    {
        var key = GetLockKey(botName, shortId);

        return await _database.StringSetAsync(
            key,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ttl,
            When.NotExists);
    }

    public Task ReleaseAsync(string botName, string shortId)
    {
        var key = GetLockKey(botName, shortId);

        return _database.KeyDeleteAsync(key);
    }

    private RedisKey GetLockKey(string botName, string shortId)
    {
        var key = RedisKeys.PositionLock(botName, shortId);

        return string.IsNullOrWhiteSpace(_options.Prefix)
            ? key
            : $"{_options.Prefix}:{key}";
    }
}