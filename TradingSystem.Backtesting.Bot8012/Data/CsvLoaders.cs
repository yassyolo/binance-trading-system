using System.Globalization;
using TradingSystem.Backtesting.Bot8012.Models;
namespace TradingSystem.Backtesting.Bot8012.Data;
public static class CsvLoaders
{
    public static IReadOnlyList<Candle> Candles(string path) => File.ReadLines(path).Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => { var p = x.Split(','); return new Candle(DateTime.Parse(p[0], null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), DateTime.Parse(p[1], null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), D(p[2]), D(p[3]), D(p[4]), D(p[5]), D(p[6])); }).ToArray();
    public static IReadOnlyList<HistoricalSignal> Signals(string path) => File.ReadLines(path).Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => { var p = x.Split(','); return new HistoricalSignal(DateTime.Parse(p[0], null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), Enum.Parse<BacktestSide>(p[1], true), p.Length > 2 ? p[2] : "recorded", p.Length > 3 ? p[3] : null); }).ToArray();
    private static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);
}
