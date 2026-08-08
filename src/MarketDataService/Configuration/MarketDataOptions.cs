namespace MarketDataService.Configuration;

public sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";

    public string[] Symbols { get; set; } = ["BTCUSDC"];

    public string[] Intervals { get; set; } = ["1m"];

    public string BinanceWebSocketBaseUrl { get; set; } =
        "wss://fstream.binance.com/market/stream";

    public int ReconnectDelaySeconds { get; set; } = 5;

    public int MaximumReconnectDelaySeconds { get; set; } = 60;

    public int KeepAliveIntervalSeconds { get; set; } = 30;

    public int ReceiveBufferSizeBytes { get; set; } = 16_384;

    public int LatestKlineTtlSeconds { get; set; } = 86_400;
}