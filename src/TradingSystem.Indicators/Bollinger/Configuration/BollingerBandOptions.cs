namespace TradingSystem.Indicators.Bollinger.Configuration;

public sealed class BollingerBandOptions
{
    public string Name { get; set; } = string.Empty;

    public int Length { get; set; }

    public string Source { get; set; } = string.Empty;

    public decimal Multiplier { get; set; }
}
