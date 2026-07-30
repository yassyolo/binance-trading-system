using System.Globalization;
using System.Text.Json;
using TradingSystem.Application.MarketData;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Binance.Market;

public sealed class BinanceHistoricalCandleRangeSource(HttpClient httpClient) : IHistoricalCandleRangeSource
{
    private const int PageLimit = 1500;

    public async Task<IReadOnlyList<MarketCandle>> LoadAsync(string symbol, string interval, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(interval);

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedInterval = interval.Trim().ToLowerInvariant();
        var from = EnsureUtc(fromUtc ?? DateTime.UtcNow.AddMonths(-3));
        var to = EnsureUtc(toUtc ?? DateTime.UtcNow);

        if (to <= from)
            throw new ArgumentException("The historical range end must be after its start.");

        var cursor = new DateTimeOffset(from).ToUnixTimeMilliseconds();
        var end = new DateTimeOffset(to).ToUnixTimeMilliseconds();
        var result = new List<MarketCandle>();

        while (cursor < end)
        {
            ct.ThrowIfCancellationRequested();
           
            var url = $"fapi/v1/klines?symbol={Uri.EscapeDataString(normalizedSymbol)}" +
                      $"&interval={Uri.EscapeDataString(normalizedInterval)}" +
                      $"&startTime={cursor}&endTime={end}&limit={PageLimit}";

            using var response = await httpClient.GetAsync(url, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new BinanceApiException(response.StatusCode, body, "load historical candle range");

            using var document = JsonDocument.Parse(body);
            var rows = document.RootElement.EnumerateArray().ToArray();
            if (rows.Length == 0)
                break;

            foreach (var row in rows)
            {
                result.Add(new MarketCandle(
                    normalizedSymbol,
                    normalizedInterval,
                    DateTimeOffset.FromUnixTimeMilliseconds(row[0].GetInt64()).UtcDateTime,
                    DateTimeOffset.FromUnixTimeMilliseconds(row[6].GetInt64()).UtcDateTime,
                    ParseDecimal(row[1]),
                    ParseDecimal(row[2]),
                    ParseDecimal(row[3]),
                    ParseDecimal(row[4]),
                    ParseDecimal(row[5]),
                    true));
            }

            var nextCursor = rows[^1][0].GetInt64() + 1;
            if (nextCursor <= cursor || rows.Length < PageLimit)
                break;

            cursor = nextCursor;
        }

        return result.Where(candle => candle.OpenTimeUtc >= from && candle.OpenTimeUtc < to)
            .OrderBy(candle => candle.OpenTimeUtc)
            .GroupBy(candle => candle.OpenTimeUtc)
            .Select(group => group.Last())
            .ToArray();
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static decimal ParseDecimal(JsonElement value)
        => decimal.Parse(value.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
}
