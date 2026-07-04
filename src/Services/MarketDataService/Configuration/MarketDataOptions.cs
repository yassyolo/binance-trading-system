namespace MarketDataService.Configuration;

public sealed class MarketDataOptions
{
    public string[] Symbols { get; set; } = ["BTCUSDC"];
    public string[] Intervals { get; set; } = ["1m"];
    public string BinanceWebSocketBaseUrl { get; set; } = "wss://fstream.binance.com/market/stream";
    public int ReconnectDelaySeconds { get; set; } = 5;
}