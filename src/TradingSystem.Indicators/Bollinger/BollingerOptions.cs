namespace TradingSystem.Indicators.Bollinger;

public sealed class BollingerOptions
{
    public const string SectionName = "Indicators:Bollinger";

    public bool Enabled { get; set; } = true;

    public string[] Symbols { get; set; } = [];

    public string[] Intervals { get; set; } = [];

    public int HistoryLimit { get; set; } = 200;

    public List<BollingerBandOptions> Bands { get; set; } = [];
}

public sealed class BollingerBandOptions
{
    public string Name { get; set; } = string.Empty;

    public int Length { get; set; }

    public string Source { get; set; } = string.Empty;

    public decimal Multiplier { get; set; }
}