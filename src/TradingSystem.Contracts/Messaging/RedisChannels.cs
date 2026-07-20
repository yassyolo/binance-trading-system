namespace TradingSystem.Contracts.Messaging;

public static class RedisChannels
{
    public const string StrategySignals  =  "trading:signals";
    public const string UserStreamOrder  =  "binance:userstream:order";
    public const string UserStreamAccount  =  "binance:userstream:account";
    public const string UserStreamRaw  =  "binance:userstream:raw";
    public const string Healing  =  "binance:system:healing";
    public const string AlligatorMa  =  "indicator_channel:alligator_ma";
    public const string Bollinger  =  "indicator_channel:bb";

    public static string Indicator(string indicatorName)
         =>  $"indicator_channel:{indicatorName.Trim().ToLowerInvariant()}";

    public static string Kline(string interval,  string symbol)
         =>  $"futures_kline_channel:{NormalizeInterval(interval)}:{NormalizeSymbol(symbol)}";

    private static string NormalizeInterval(string value)  =>  value.Trim().ToLowerInvariant();
    private static string NormalizeSymbol(string value)  =>  value.Trim().ToUpperInvariant();
}
