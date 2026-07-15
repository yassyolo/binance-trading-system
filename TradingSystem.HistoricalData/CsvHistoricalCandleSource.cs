using System.Globalization;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.HistoricalData;

public sealed class CsvHistoricalCandleSource(string filePath) : IHistoricalCandleSource
{
    public async Task<IReadOnlyList<HistoricalCandle>> LoadAsync(string symbol, string interval, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("Historical CSV not found.", filePath);
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length < 2) return [];
        var separator = lines[0].Contains(';') ? ';' : ',';
        var headers = lines[0].Split(separator).Select(x => x.Trim().ToLowerInvariant()).ToArray();
        int Col(params string[] names) => Array.FindIndex(headers, h => names.Contains(h, StringComparer.OrdinalIgnoreCase));
        var ti = Col("time", "timestamp", "datetime"); var oi = Col("open", "o"); var hi = Col("high", "h");
        var li = Col("low", "l"); var ci = Col("close", "c"); var vi = Col("volume", "v");
        if (new[] { ti, oi, hi, li, ci }.Any(x => x < 0)) throw new InvalidOperationException("CSV requires time,open,high,low,close columns.");
        var result = new List<HistoricalCandle>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var p = line.Split(separator);
            if (!DateTime.TryParse(p[ti], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var time)) continue;
            if (fromUtc.HasValue && time < fromUtc.Value || toUtc.HasValue && time > toUtc.Value) continue;
            result.Add(new HistoricalCandle
            {
                Symbol = symbol,
                Interval = interval,
                OpenTimeUtc = DateTime.SpecifyKind(time, DateTimeKind.Utc),
                CloseTimeUtc = DateTime.SpecifyKind(time, DateTimeKind.Utc),
                Open = D(p[oi]),
                High = D(p[hi]),
                Low = D(p[li]),
                Close = D(p[ci]),
                Volume = vi >= 0 && vi < p.Length ? D(p[vi]) : 0m
            });
        }
        return result.OrderBy(x => x.OpenTimeUtc).GroupBy(x => x.OpenTimeUtc).Select(x => x.Last()).ToArray();
    }
    private static decimal D(string value) => decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
}
