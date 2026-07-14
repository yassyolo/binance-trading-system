using StackExchange.Redis;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Redis;

internal static class RedisKeyFactory
{
    public static RedisKey Cooldown(string botName, string symbol, PositionSide side)
        => $"trading:cooldown:{Normalize(botName)}:{Normalize(symbol)}:{Normalize(side.ToString())}";

    public static RedisKey SignalIdempotency(string signalId)
        => $"trading:signal-idempotency:{signalId.Trim()}";

    public static RedisKey OperationLock(string botName, string symbol, PositionSide side)
        => $"trading:operation-lock:{Normalize(botName)}:{Normalize(symbol)}:{Normalize(side.ToString())}";

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
