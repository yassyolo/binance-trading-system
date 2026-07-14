using System.Globalization;
using StackExchange.Redis;
using TradingSystem.Application.Engine;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Redis.Engine;

public sealed class RedisSignalCooldownStore(IConnectionMultiplexer redis)
    : ISignalCooldownStore
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<TimeSpan?> GetRemainingAsync(
        string botName,
        string symbol,
        PositionSide side,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = RedisKeyFactory.Cooldown(botName, symbol, side);
        var value = await _database.StringGetAsync(key);

        if (!value.HasValue)
            return null;

        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixMilliseconds))
        {
            await _database.KeyDeleteAsync(key);
            return null;
        }

        var remaining = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).UtcDateTime - nowUtc;
        if (remaining > TimeSpan.Zero)
            return remaining;

        await _database.KeyDeleteAsync(key);
        return null;
    }

    public Task SetAsync(
        string botName,
        string symbol,
        PositionSide side,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ttl = expiresAtUtc - DateTime.UtcNow;

        if (ttl <= TimeSpan.Zero)
            return Task.CompletedTask;

        var value = new DateTimeOffset(expiresAtUtc)
            .ToUnixTimeMilliseconds()
            .ToString(CultureInfo.InvariantCulture);

        return _database.StringSetAsync(
            RedisKeyFactory.Cooldown(botName, symbol, side),
            value,
            ttl);
    }
}
