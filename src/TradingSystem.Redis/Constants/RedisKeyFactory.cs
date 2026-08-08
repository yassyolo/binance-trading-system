using StackExchange.Redis;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Redis.Constants;

public sealed class RedisKeyFactory(string prefix)
{
    private readonly string _prefix = string.IsNullOrWhiteSpace(prefix)
        ? string.Empty : prefix.Trim().TrimEnd(':') + ":";

    public RedisKey Cooldown(string bot, string symbol, PositionSide side)
        => Key($"trading:cooldown:{Normalize(bot)}:{Normalize(symbol)}:{Normalize(side.ToString())}");

    public RedisKey SignalIdempotency(string signalId)
        => Key($"trading:signal-idempotency:{signalId.Trim()}");

    public RedisKey OperationLock(string bot, string symbol, PositionSide side)
        => Key($"trading:operation-lock:{Normalize(bot)}:{Normalize(symbol)}:{Normalize(side.ToString())}");

    public RedisKey Position(string bot, string shortId)
        => Key($"{Normalize(bot)}:position:{shortId.Trim()}");

    public RedisKey PositionIndex(string bot)
        => Key($"{Normalize(bot)}:positions");

    public RedisValue PositionPattern(string bot)
        => Key($"{Normalize(bot)}:position:*").ToString();

    private RedisKey Key(string value) 
        => _prefix + value;

    private static string Normalize(string value)
        => value.Trim().ToUpperInvariant();
}
