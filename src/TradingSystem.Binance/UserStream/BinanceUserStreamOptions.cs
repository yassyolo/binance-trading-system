namespace TradingSystem.Binance.UserStream;

public sealed class BinanceUserStreamOptions
{
    public const string SectionName = "BinanceUserStream";

    public string RestBaseUrl { get; set; } = "https://fapi.binance.com";
    public string WebSocketBaseUrl { get; set; } = "wss://fstream.binance.com";
    public string ApiKey { get; set; } = string.Empty;
    public int ListenKeyKeepAliveSeconds { get; set; } = 1800;
    public int KeepAliveIntervalSeconds { get; set; } = 30;
    public int ReceiveBufferSizeBytes { get; set; } = 32768;
}
