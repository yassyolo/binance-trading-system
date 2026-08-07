namespace TradingSystem.Indicators.Bollinger.Configuration;

public sealed class BollingerOptions
{
    public const string SectionName = "Indicators:Bollinger";

    public bool Enabled { get; set; } = true;

    public string[] Symbols { get; set; } = [];

    public string[] Intervals { get; set; } = [];

    public int HistoryLimit { get; set; } = 200;

    public List<BollingerBandOptions> Bands { get; set; } = [];
}