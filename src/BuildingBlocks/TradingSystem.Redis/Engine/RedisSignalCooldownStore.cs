using System.Globalization;
using StackExchange.Redis;
using TradingSystem.Application.Engine;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Redis.Engine;

public sealed class RedisSignalCooldownStore(
    IConnectionMultiplexer connectionMultiplexer)
    : ISignalCooldownStore
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<TimeSpan?> GetRemainingAsync(
        string botName,
        string symbol,
        PositionSide side,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var value = await _database.StringGetAsync(
            GetKey(botName, symbol, side));

        if (!value.HasValue)
            return null;

        if (!long.TryParse(
                value.ToString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var unixMilliseconds))
        {
            await _database.KeyDeleteAsync(
                GetKey(botName, symbol, side));

            return null;
        }

        var expiresAtUtc = DateTimeOffset
            .FromUnixTimeMilliseconds(unixMilliseconds)
            .UtcDateTime;

        var remaining = expiresAtUtc - nowUtc;

        return remaining > TimeSpan.Zero
            ? remaining
            : null;
    }

    public Task SetAsync(
        string botName,
        string symbol,
        PositionSide side,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var ttl = expiresAtUtc - DateTime.UtcNow;

        if (ttl <= TimeSpan.Zero)
            return Task.CompletedTask;

        var value = new DateTimeOffset(expiresAtUtc)
            .ToUnixTimeMilliseconds()
            .ToString(CultureInfo.InvariantCulture);

        return _database.StringSetAsync(
            GetKey(botName, symbol, side),
            value,
            ttl);
    }

    private static RedisKey GetKey(
        string botName,
        string symbol,
        PositionSide side)
        => $"trading:cooldown:{botName.ToUpperInvariant()}:{symbol.ToUpperInvariant()}:{side.ToString().ToUpperInvariant()}";
}
