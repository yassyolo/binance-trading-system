using System.Globalization;
using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Bot8011.Models;

namespace TradingSystem.Backtesting.Bot8011.Signals;

public sealed class CsvBot8011SignalSource(string filePath) : IBot8011SignalSource
{
    public async Task<IReadOnlyList<Bot8011Signal>> LoadAsync(IReadOnlyList<HistoricalCandle> candles, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("Signal CSV was not found.", filePath);
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        var result = new List<Bot8011Signal>();
        foreach (var raw in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var parts = raw.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2) continue;
            if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var time)) continue;
            var side = parts[1].Equals("LONG", StringComparison.OrdinalIgnoreCase) || parts[1].Equals("BUY", StringComparison.OrdinalIgnoreCase)
                ? TradeSide.Long : TradeSide.Short;
            result.Add(new Bot8011Signal(time, side, parts.Length > 2 ? parts[2] : "csv"));
        }
        return result.OrderBy(x => x.TimeUtc).ToArray();
    }
}
