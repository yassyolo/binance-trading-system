namespace AlligatorIndicatorService.Configuration;

public sealed class AlligatorOptions
{
    public const string SectionName = "Alligator";

    public string[] Symbols { get; set; } = ["BTCUSDC"];
    public string[] Intervals { get; set; } = ["5m"];

    public int HistoryLimit { get; set; } = 300;
    public int SmaLength { get; set; } = 200;

    public int JawLength { get; set; } = 13;
    public int TeethLength { get; set; } = 8;
    public int LipsLength { get; set; } = 5;

    public string BinanceKlinesUrl { get; set; }
        = "https://fapi.binance.com/fapi/v1/klines";

    public string RedisOutputChannel { get; set; }
        = "indicator_channel:alligator_ma";

    public bool PublishOnlyClosedCandles { get; set; } = true;
}
