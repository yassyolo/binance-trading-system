namespace TradingSystem.Contracts.Redis;

public static class RedisChannels
{
    public const string StrategySignals = "trading:signals";

    public const string UserStreamOrder = "binance:userstream:order";
    public const string UserStreamAccount = "binance:userstream:account";
    public const string UserStreamRaw = "binance:userstream:raw";
    public const string Healing = "binance:system:healing";

    public const string AlligatorMa = "indicator_channel:alligator_ma";
    public const string Bollinger = "indicator_channel:bb";

    public static string Kline(
        string interval,
        string symbol)
    {
        return $"futures_kline_channel:{interval.ToLowerInvariant()}:{symbol.ToUpperInvariant()}";
    }
}