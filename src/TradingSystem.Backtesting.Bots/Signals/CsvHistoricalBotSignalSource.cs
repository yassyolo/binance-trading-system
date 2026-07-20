using System.Globalization;
using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Bots.Signals;

public sealed class CsvHistoricalBotSignalSource(string filePath)
{
    public async Task<IReadOnlyList<HistoricalBotSignal>> LoadAsync(CancellationToken cancellationToken  =  default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("Signal CSV was not found.",  filePath);
        var lines  =  await File.ReadAllLinesAsync(filePath,  cancellationToken);
        if (lines.Length == 0) return [];
        var separator  =  lines[0].Contains(';') ? ';' : ', ';
        var result  =  new List<HistoricalBotSignal>();
        foreach (var raw in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var parts  =  raw.Split(separator,  StringSplitOptions.TrimEntries);
            if (parts.Length < 2  ||  !DateTime.TryParse(parts[0],  CultureInfo.InvariantCulture,  DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,  out var time)) continue;
            if (!TrySide(parts[1],  out var side)) continue;
            result.Add(new HistoricalBotSignal(time,  side,  parts.Length > 2 ? parts[2] : "csv",  parts.Length > 3 ? parts[3] : null));
        }
        return result.OrderBy(x  =>  x.TimeUtc).ToArray();
    }

    private static bool TrySide(string value,  out TradeSide side)
    {
        if (value.Equals("LONG",  StringComparison.OrdinalIgnoreCase)  ||  value.Equals("BUY",  StringComparison.OrdinalIgnoreCase)) { side  =  TradeSide.Long; return true; }
        if (value.Equals("SHORT",  StringComparison.OrdinalIgnoreCase)  ||  value.Equals("SELL",  StringComparison.OrdinalIgnoreCase)) { side  =  TradeSide.Short; return true; }
        side  =  default; return false;
    }
}
