using System.Globalization;
using System.Text.Json;
using AlligatorIndicatorService.Configuration;
using AlligatorIndicatorService.Models;
using Microsoft.Extensions.Options;

namespace AlligatorIndicatorService.Services;

public sealed class BinanceHistoricalKlineClient(
    HttpClient httpClient,
    IOptions<AlligatorOptions> options)
{
    public async Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(
        string symbol,
        string interval,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"{options.Value.BinanceKlinesUrl}?symbol={symbol}&interval={interval}&limit={limit}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        return document.RootElement
            .EnumerateArray()
            .Select(kline => new Candle
            {
                Symbol = symbol.ToUpperInvariant(),
                Interval = interval.ToLowerInvariant(),
                OpenTime = kline[0].GetInt64(),
                Open = ParseDecimal(kline[1]),
                High = ParseDecimal(kline[2]),
                Low = ParseDecimal(kline[3]),
                Close = ParseDecimal(kline[4]),
                CloseTime = kline[6].GetInt64(),
                IsClosed = true
            })
            .ToArray();
    }

    private static decimal ParseDecimal(JsonElement element)
        => decimal.Parse(
            element.GetString()!,
            NumberStyles.Any,
            CultureInfo.InvariantCulture);
}
