namespace BollingerIndicatorService.Configuration;

public sealed class BollingerOptions
{
    public string[] Symbols { get; set; } = ["BTCUSDC"];
    public string[] Intervals { get; set; } = ["30m", "1h"];
    public int HistoryLimit { get; set; } = 200;

    public string BinanceKlinesUrl { get; set; } = "https://fapi.binance.com/fapi/v1/klines";

    public List<BollingerBandOptions> Bands { get; set; } =
    [
        new()
        {
            Name = "BB4_OPEN",
            Length = 4,
            Source = "open",
            Mult = 4,
            MaType = "SMA"
        },
        new()
        {
            Name = "BB20_CLOSE",
            Length = 20,
            Source = "close",
            Mult = 2,
            MaType = "SMA"
        }
    ];
}

public sealed class BollingerBandOptions
{
    public string Name { get; set; } = string.Empty;
    public int Length { get; set; }
    public string Source { get; set; } = "close";
    public decimal Mult { get; set; } = 2;
    public string MaType { get; set; } = "SMA";
}