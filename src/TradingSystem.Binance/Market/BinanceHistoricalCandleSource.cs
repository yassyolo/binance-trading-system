using System.Globalization;
using System.Text.Json;
using TradingSystem.Application.MarketData;
using TradingSystem.Binance.Exceptions;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Binance.Market;

public sealed class BinanceHistoricalCandleSource(
    HttpClient httpClient) 
    : IHistoricalCandleSource
{
    public async Task<IReadOnlyList<MarketCandle>> LoadLatestAsync(string symbol, string interval, int limit, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(interval);
        if (limit is < 1 or > 1500)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be between 1 and 1500.");

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var normalizedInterval = interval.Trim().ToLowerInvariant();
        var url = $"fapi/v1/klines?symbol={Uri.EscapeDataString(normalizedSymbol)}" +
                  $"&interval={Uri.EscapeDataString(normalizedInterval)}&limit={limit}";

        using var response = await httpClient.GetAsync(url, ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new BinanceApiException(response.StatusCode, content, "load latest candles");

        using var document = JsonDocument.Parse(content);
        return document.RootElement
            .EnumerateArray()
            .Select(row => new MarketCandle(
                normalizedSymbol,
                normalizedInterval,
                DateTimeOffset.FromUnixTimeMilliseconds(row[0].GetInt64()).UtcDateTime,
                DateTimeOffset.FromUnixTimeMilliseconds(row[6].GetInt64()).UtcDateTime,
                ParseDecimal(row[1]),
                ParseDecimal(row[2]),
                ParseDecimal(row[3]),
                ParseDecimal(row[4]),
                ParseDecimal(row[5]),
                true))
            .OrderBy(candle => candle.OpenTimeUtc)
            .ToArray();
    }

    private static decimal ParseDecimal(JsonElement value)
        => decimal.Parse(value.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
}
