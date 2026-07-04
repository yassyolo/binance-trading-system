namespace TradingSystem.Contracts.Redis;

public static class RedisKeys
{
    public static string Position(
        string botName,
        string shortId)
        => $"{botName}:position:{shortId}";

    public static string PositionLock(
        string botName,
        string shortId)
        => $"{Position(botName, shortId)}:lock";

    public static string Kline(
        string symbol,
        string interval)
        => $"kline:{symbol.ToLowerInvariant()}:{interval}";
}