namespace TradingSystem.Contracts.Redis;

public static class RedisNames
{
    public static string KlineKey(string symbol, string interval)
        => $"kline:{symbol.ToLowerInvariant()}:{interval}";

    public static string KlineChannel(string interval, string symbol)
        => $"futures_kline_channel:{interval}:{symbol.ToUpperInvariant()}";

    public static string BinanceEventChannel(string symbol)
        => $"binance_futures_events:{symbol.ToUpperInvariant()}";

    public static string BotPositionKey(string botName, string shortId)
    => $"{botName}:position:{shortId}";

    public static string BotPositionLockKey(string botName, string shortId)
        => $"{BotPositionKey(botName, shortId)}:lock";

    public const string UserStreamOrder = "binance:userstream:order";
    public const string UserStreamAccount = "binance:userstream:account";
    public const string UserStreamRaw = "binance:userstream:raw";

    public const string HealingChannel = "binance:system:healing";

    public const string AlligatorMaChannel = "indicator_channel:alligator_ma";
}