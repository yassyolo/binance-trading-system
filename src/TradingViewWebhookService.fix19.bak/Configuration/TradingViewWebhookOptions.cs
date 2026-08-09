namespace TradingViewWebhookService.Configuration;

public sealed class TradingViewWebhookOptions
{
    public const string SectionName = "TradingView";

    public bool Enabled { get; set; } = true;

    public string Secret { get; set; } = string.Empty;

    public Dictionary<string, TradingViewBotOptions> Bots { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
