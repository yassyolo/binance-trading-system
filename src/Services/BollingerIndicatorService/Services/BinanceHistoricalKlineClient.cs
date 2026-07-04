using System.Globalization;
using System.Text.Json;
using BollingerIndicatorService.Configuration;
using BollingerIndicatorService.Models;
using Microsoft.Extensions.Options;

namespace BollingerIndicatorService.Services;

public sealed class BinanceHistoricalKlineClient(
    HttpClient httpClient,
    IOptions<BollingerOptions> options)
{
    private readonly BollingerOptions options = options.Value;

    public async Task<List<BollingerCandle>> GetHistoricalCandlesAsync(string symbol, string interval, int limit, CancellationToken cancellationToken = default)
    {
        var url = $"{options.BinanceKlinesUrl}?symbol={symbol}&interval={interval}&limit={limit}";

        using var response = await httpClient.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var candles = new List<BollingerCandle>();

        foreach (var kline in document.RootElement.EnumerateArray())
        {
            candles.Add(new BollingerCandle
            {
                Time = kline[0].GetInt64(),
                Open = ParseDecimal(kline[1]),
                Close = ParseDecimal(kline[4]),
                CloseTime = kline[6].GetInt64()
            });
        }

        return candles.OrderBy(x => x.Time).ToList();
    }

    private static decimal ParseDecimal(JsonElement element)
        => decimal.Parse(element.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
}