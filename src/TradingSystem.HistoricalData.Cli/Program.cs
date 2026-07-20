using System.Globalization;
using TradingSystem.Binance.Market;

var options  =  Parse(args);
var symbol  =  Get("symbol",  "BTCUSDC").ToUpperInvariant();
var interval  =  Get("interval",  "1m").ToLowerInvariant();
var fromUtc  =  ParseUtc(Get("from",  DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd",  CultureInfo.InvariantCulture)));
var toUtc  =  ParseUtc(Get("to",  DateTime.UtcNow.ToString("yyyy-MM-dd",  CultureInfo.InvariantCulture)));
var output  =  Get("output",  $"data/{symbol}_{interval}_{fromUtc:yyyyMMdd}_{toUtc:yyyyMMdd}.csv");

using var http  =  new HttpClient { BaseAddress  =  new Uri("https://fapi.binance.com"),  Timeout  =  TimeSpan.FromSeconds(30) };
var source  =  new BinanceHistoricalCandleRangeSource(http);
var candles  =  await source.LoadAsync(symbol,  interval,  fromUtc,  toUtc);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
await using var writer  =  new StreamWriter(output,  false);
await writer.WriteLineAsync("open_time_utc, close_time_utc, open, high, low, close, volume");
foreach (var candle in candles)
{
    await writer.WriteLineAsync(string.Join(',', 
        candle.OpenTimeUtc.ToString("O",  CultureInfo.InvariantCulture), 
        candle.CloseTimeUtc.ToString("O",  CultureInfo.InvariantCulture), 
        F(candle.Open),  F(candle.High),  F(candle.Low),  F(candle.Close),  F(candle.Volume)));
}
Console.WriteLine($"Completed. Candles = {candles.Count:N0}; File = {Path.GetFullPath(output)}");

string Get(string key,  string fallback)  =>  options.TryGetValue(key,  out var value) ? value : fallback;
static string F(decimal value)  =>  value.ToString(CultureInfo.InvariantCulture);
static DateTime ParseUtc(string value)
{
    if (!DateTime.TryParse(value,  CultureInfo.InvariantCulture, 
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,  out var result))
        throw new ArgumentException($"Invalid UTC date: {value}");
    return DateTime.SpecifyKind(result,  DateTimeKind.Utc);
}
static Dictionary<string,  string> Parse(string[] values)
{
    var result  =  new Dictionary<string,  string>(StringComparer.OrdinalIgnoreCase);
    for (var index  =  0; index < values.Length; index++)
    {
        if (!values[index].StartsWith("--",  StringComparison.Ordinal)) continue;
        var key  =  values[index][2..];
        result[key]  =  index + 1 < values.Length  &&  !values[index + 1].StartsWith("--",  StringComparison.Ordinal)
            ? values[++index] : "true";
    }
    return result;
}
