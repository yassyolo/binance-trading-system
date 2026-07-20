namespace TradingSystem.Indicators.Bollinger;
public sealed class BollingerOptions
{
 public const string SectionName = "Indicators:Bollinger";
 public bool Enabled{ get; set;} = true; public string[] Symbols{ get; set;} = ["BTCUSDC"]; public string[] Intervals{ get; set;} = ["30m", "1h"]; public int HistoryLimit{ get; set;} = 200;
 public List<BollingerBandOptions> Bands{ get; set;} = [new(){Name = "BB4_OPEN", Length = 4, Source = "open", Multiplier = 4}, new(){Name = "BB20_CLOSE", Length = 20, Source = "close", Multiplier = 2}];
}
public sealed class BollingerBandOptions { public string Name{ get; set;} = ""; public int Length{ get; set;} = 20; public string Source{ get; set;} = "close"; public decimal Multiplier{ get; set;} = 2; }
