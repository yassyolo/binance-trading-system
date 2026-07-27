namespace MarketDataService.Configuration;

public sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";
    public const string ValidationError =
        "MarketData requires symbols, intervals, a valid ws/wss Binance URL, positive reconnect/keep-alive/buffer settings, and a positive latest-kline TTL.";

    public string[] Symbols { get; set; } = ["BTCUSDC"];
    public string[] Intervals { get; set; } = ["1m"];
    public string BinanceWebSocketBaseUrl { get; set; } =
        "wss://fstream.binance.com/market/stream";
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int MaximumReconnectDelaySeconds { get; set; } = 60;
    public int KeepAliveIntervalSeconds { get; set; } = 30;
    public int ReceiveBufferSizeBytes { get; set; } = 16_384;
    public int LatestKlineTtlSeconds { get; set; } = 86_400;

    public static bool IsValid(MarketDataOptions options)
    {
        var validUri = Uri.TryCreate(
            options.BinanceWebSocketBaseUrl,
            UriKind.Absolute,
            out var uri) &&
            (uri.Scheme == Uri.UriSchemeWs || uri.Scheme == Uri.UriSchemeWss);

        return options.Symbols.Any(x => !string.IsNullOrWhiteSpace(x)) &&
               options.Intervals.Any(x => !string.IsNullOrWhiteSpace(x)) &&
               validUri &&
               options.ReconnectDelaySeconds > 0 &&
               options.MaximumReconnectDelaySeconds >= options.ReconnectDelaySeconds &&
               options.KeepAliveIntervalSeconds > 0 &&
               options.ReceiveBufferSizeBytes >= 1_024 &&
               options.LatestKlineTtlSeconds > 0;
    }
}
