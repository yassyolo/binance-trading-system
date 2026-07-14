namespace UserStreamService.Configuration;

public sealed class BinanceUserStreamOptions
{
    public const string SectionName = "Binance";

    public string BaseUrl { get; set; } = "https://fapi.binance.com";
    public string UserStreamWebSocketUrl { get; set; } = "wss://fstream.binance.com/private/ws";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public int ReceiveWindow { get; set; } = 5000;
}
