using System.Globalization;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Bots.Signals;

public sealed class CsvHistoricalBotSignalSource(string filePath)
{
    public async Task<IReadOnlyList<HistoricalBotSignal>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Signal CSV was not found.", filePath);

        var result = new List<HistoricalBotSignal>();
        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);

        var header = await reader.ReadLineAsync(ct);
        if (header is null)
            return [];

        var separator = header.Contains(';') ? ';' : ',';
        var lineNumber = 1;

        while (await reader.ReadLineAsync(ct) is { } raw)
        {
            lineNumber++;
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var parts = raw.Split(separator, StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                throw new InvalidDataException($"Signal CSV row {lineNumber} requires at least time and side.");

            if (!DateTime.TryParse(
                    parts[0],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var time))
                throw new InvalidDataException($"Invalid signal time at row {lineNumber}: '{parts[0]}'.");

            if (!TrySide(parts[1], out var side))
                throw new InvalidDataException($"Invalid signal side at row {lineNumber}: '{parts[1]}'.");

            result.Add(new HistoricalBotSignal(
                DateTime.SpecifyKind(time, DateTimeKind.Utc),
                side,
                parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : "csv",
                parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3] : null));
        }

        return result
            .OrderBy(x => x.TimeUtc)
            .ThenBy(x => x.SignalId, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TrySide(string value, out TradeSide side)
    {
        if (value.Equals("LONG", StringComparison.OrdinalIgnoreCase)
            || value.Equals("BUY", StringComparison.OrdinalIgnoreCase))
        {
            side = TradeSide.Long;
            return true;
        }

        if (value.Equals("SHORT", StringComparison.OrdinalIgnoreCase)
            || value.Equals("SELL", StringComparison.OrdinalIgnoreCase))
        {
            side = TradeSide.Short;
            return true;
        }

        side = default;
        return false;
    }
}
