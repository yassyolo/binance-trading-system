namespace TradingViewWebhookService.Configuration;

public sealed class TradingViewBotOptions
{
    public bool Enabled { get; set; } = true;

    public string Symbol { get; set; } = "BTCUSDC";
}

