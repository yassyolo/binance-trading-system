using StackExchange.Redis;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Redis;

public sealed class RedisKeyFactory(string prefix)
{
    private readonly string _prefix  =  string.IsNullOrWhiteSpace(prefix) ? string.Empty : prefix.Trim().TrimEnd(':') + ":";
    public RedisKey Cooldown(string bot,  string symbol,  PositionSide side)  =>  Key($"trading:cooldown:{N(bot)}:{N(symbol)}:{N(side.ToString())}");
    public RedisKey SignalIdempotency(string signalId)  =>  Key($"trading:signal-idempotency:{signalId.Trim()}");
    public RedisKey OperationLock(string bot,  string symbol,  PositionSide side)  =>  Key($"trading:operation-lock:{N(bot)}:{N(symbol)}:{N(side.ToString())}");
    public RedisKey Position(string bot,  string shortId)  =>  Key($"{N(bot)}:position:{shortId.Trim()}");
    public RedisValue PositionPattern(string bot)  =>  Key($"{N(bot)}:position:*").ToString();
    private RedisKey Key(string value)  =>  _prefix + value;
    private static string N(string value)  =>  value.Trim().ToUpperInvariant();
}
