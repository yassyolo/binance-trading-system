namespace TradingSystem.Contracts.Redis;

public static class RedisKeys
{
    public static string Kline(
        string symbol,
        string interval)
    {
        return $"kline:{symbol.ToLowerInvariant()}:{interval.ToLowerInvariant()}";
    }

    public static string Position(string botName, string shortId)
        => $"{botName}:position:{shortId}";

    public static string PositionLock(string botName, string shortId)
        => $"{Position(botName, shortId)}:lock";

    public static string AlligatorState(string symbol, string interval)
        => $"indicator_state:alligator_ma:{symbol.ToUpperInvariant()}:{interval.ToLowerInvariant()}";

    public static string BollingerState(string symbol, string interval)
    => $"indicator_state:bb:{symbol.ToUpperInvariant()}:{interval.ToLowerInvariant()}";
}