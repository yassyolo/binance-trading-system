using System.Globalization;
using TradingSystem.Application.MarketData;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.HistoricalData;

public sealed class CsvHistoricalCandleSource(string filePath) : IHistoricalCandleRangeSource
{
    public async Task<IReadOnlyList<MarketCandle>> LoadAsync(string symbol, string interval, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(interval);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Historical CSV not found.", filePath);

        DateTime? normalizedFromUtc = fromUtc.HasValue ? EnsureUtc(fromUtc.Value) : (DateTime?)null;
        DateTime? normalizedToUtc = toUtc.HasValue ? EnsureUtc(toUtc.Value) : (DateTime?)null;
        if (normalizedFromUtc.HasValue && normalizedToUtc.HasValue && normalizedFromUtc >= normalizedToUtc)
            throw new ArgumentException("The historical range must satisfy fromUtc < toUtc.");

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);

        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine is null)
            return [];

        var separator = headerLine.Contains(';') ? ';' : ',';
        var headers = SplitLine(headerLine, separator)
            .Select(static value => value.Trim().ToLowerInvariant())
            .ToArray();

        int Column(params string[] names) =>
            Array.FindIndex(headers, header => names.Contains(header, StringComparer.OrdinalIgnoreCase));

        var openTimeIndex = Column("open_time_utc", "time", "timestamp", "datetime");
        var closeTimeIndex = Column("close_time_utc", "close_time");
        var openIndex = Column("open", "o");
        var highIndex = Column("high", "h");
        var lowIndex = Column("low", "l");
        var closeIndex = Column("close", "c");
        var volumeIndex = Column("volume", "v");

        var requiredIndexes = new[] { openTimeIndex, openIndex, highIndex, lowIndex, closeIndex };
        if (requiredIndexes.Any(index => index < 0))
            throw new InvalidOperationException(
                "CSV requires open_time_utc/time, open, high, low and close columns.");

        var highestRequiredIndex = requiredIndexes.Max();
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedInterval = interval.Trim().ToLowerInvariant();
        var candlesByOpenTime = new Dictionary<DateTime, MarketCandle>();

        var lineNumber = 1;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            lineNumber++;
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = SplitLine(line, separator);
            if (values.Length <= highestRequiredIndex)
                throw new FormatException($"CSV row {lineNumber} has fewer columns than the header.");

            if (!TryParseUtc(values, openTimeIndex, out var openTimeUtc))
                throw new FormatException($"CSV row {lineNumber} contains an invalid open time.");

            // Historical ranges are half-open: [fromUtc, toUtc).
            if (normalizedFromUtc.HasValue && openTimeUtc < normalizedFromUtc.Value)
                continue;
            if (normalizedToUtc.HasValue && openTimeUtc >= normalizedToUtc.Value)
                continue;

            var closeTimeUtc = TryParseUtc(values, closeTimeIndex, out var parsedClose)
                ? parsedClose
                : openTimeUtc;

            var candle = new MarketCandle(
                normalizedSymbol,
                normalizedInterval,
                openTimeUtc,
                closeTimeUtc,
                ParseDecimal(values[openIndex], lineNumber, "open"),
                ParseDecimal(values[highIndex], lineNumber, "high"),
                ParseDecimal(values[lowIndex], lineNumber, "low"),
                ParseDecimal(values[closeIndex], lineNumber, "close"),
                volumeIndex >= 0 && volumeIndex < values.Length
                    ? ParseDecimal(values[volumeIndex], lineNumber, "volume")
                    : 0m,
                true);

            ValidateCandle(candle, lineNumber);
            candlesByOpenTime[openTimeUtc] = candle;
        }

        return candlesByOpenTime.Values
            .OrderBy(static candle => candle.OpenTimeUtc)
            .ToArray();
    }

    private static string[] SplitLine(string line, char separator)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var quoted = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
                continue;
            }

            if (character == separator && !quoted)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        if (quoted)
            throw new FormatException("CSV contains an unterminated quoted value.");

        values.Add(current.ToString().Trim());
        return values.ToArray();
    }

    private static bool TryParseUtc(string[] values, int index, out DateTime utc)
    {
        utc = default;
        if (index < 0 || index >= values.Length)
            return false;

        if (!DateTime.TryParse(
                values[index],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return false;

        utc = EnsureUtc(parsed);
        return true;
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value.ToUniversalTime()
    };

    private static decimal ParseDecimal(string value, int lineNumber, string column)
    {
        if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw new FormatException($"CSV row {lineNumber} contains an invalid {column} value '{value}'.");
    }

    private static void ValidateCandle(MarketCandle candle, int lineNumber)
    {
        if (candle.CloseTimeUtc < candle.OpenTimeUtc)
            throw new FormatException($"CSV row {lineNumber} has close_time_utc before open_time_utc.");
        if (candle.Low > candle.High)
            throw new FormatException($"CSV row {lineNumber} has low greater than high.");
        if (candle.Open < candle.Low || candle.Open > candle.High ||
            candle.Close < candle.Low || candle.Close > candle.High)
            throw new FormatException($"CSV row {lineNumber} has OHLC values outside the low/high range.");
        if (candle.Volume < 0)
            throw new FormatException($"CSV row {lineNumber} has negative volume.");
    }
}
