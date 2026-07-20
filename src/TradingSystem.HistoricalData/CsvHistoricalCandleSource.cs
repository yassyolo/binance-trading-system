using System.Globalization;
using TradingSystem.Application.MarketData;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.HistoricalData;

public sealed class CsvHistoricalCandleSource(string filePath) : IHistoricalCandleRangeSource
{
    public async Task<IReadOnlyList<MarketCandle>> LoadAsync(
        string symbol, 
        string interval, 
        DateTime? fromUtc, 
        DateTime? toUtc, 
        CancellationToken cancellationToken  =  default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Historical CSV not found.",  filePath);

        var lines  =  await File.ReadAllLinesAsync(filePath,  cancellationToken);
        if (lines.Length < 2)
            return [];

        var separator  =  lines[0].Contains(';') ? ';' : ', ';
        var headers  =  lines[0].Split(separator)
            .Select(static value  =>  value.Trim().ToLowerInvariant())
            .ToArray();

        int Column(params string[] names)
             =>  Array.FindIndex(headers,  header  =>  names.Contains(header,  StringComparer.OrdinalIgnoreCase));

        var openTimeIndex  =  Column("open_time_utc",  "time",  "timestamp",  "datetime");
        var closeTimeIndex  =  Column("close_time_utc",  "close_time");
        var openIndex  =  Column("open",  "o");
        var highIndex  =  Column("high",  "h");
        var lowIndex  =  Column("low",  "l");
        var closeIndex  =  Column("close",  "c");
        var volumeIndex  =  Column("volume",  "v");

        if (new[] { openTimeIndex,  openIndex,  highIndex,  lowIndex,  closeIndex }.Any(index  =>  index < 0))
            throw new InvalidOperationException("CSV requires open_time_utc/time,  open,  high,  low and close columns.");

        var normalizedSymbol  =  symbol.Trim().ToUpperInvariant();
        var normalizedInterval  =  interval.Trim().ToLowerInvariant();
        var result  =  new List<MarketCandle>();

        foreach (var line in lines.Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values  =  line.Split(separator);
            if (!TryParseUtc(values,  openTimeIndex,  out var openTimeUtc))
                continue;
            if (fromUtc.HasValue  &&  openTimeUtc < EnsureUtc(fromUtc.Value))
                continue;
            if (toUtc.HasValue  &&  openTimeUtc > EnsureUtc(toUtc.Value))
                continue;

            var closeTimeUtc  =  TryParseUtc(values,  closeTimeIndex,  out var parsedClose)
                ? parsedClose
                : openTimeUtc;

            result.Add(new MarketCandle(
                normalizedSymbol, 
                normalizedInterval, 
                openTimeUtc, 
                closeTimeUtc, 
                ParseDecimal(values[openIndex]), 
                ParseDecimal(values[highIndex]), 
                ParseDecimal(values[lowIndex]), 
                ParseDecimal(values[closeIndex]), 
                volumeIndex >= 0  &&  volumeIndex < values.Length ? ParseDecimal(values[volumeIndex]) : 0m, 
                true));
        }

        return result
            .OrderBy(static candle  =>  candle.OpenTimeUtc)
            .GroupBy(static candle  =>  candle.OpenTimeUtc)
            .Select(static group  =>  group.Last())
            .ToArray();
    }

    private static bool TryParseUtc(string[] values,  int index,  out DateTime utc)
    {
        utc  =  default;
        if (index < 0  ||  index >= values.Length)
            return false;
        if (!DateTime.TryParse(values[index],  CultureInfo.InvariantCulture, 
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,  out var parsed))
            return false;
        utc  =  EnsureUtc(parsed);
        return true;
    }

    private static DateTime EnsureUtc(DateTime value)
         =>  value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    private static decimal ParseDecimal(string value)
         =>  decimal.Parse(value,  NumberStyles.Any,  CultureInfo.InvariantCulture);
}
