using System.Text.Json;
using AlligatorIndicatorService.Models;

namespace AlligatorIndicatorService.Services;

public sealed class BinanceHistoricalKlineClient
{
    private readonly HttpClient _httpClient;

    public BinanceHistoricalKlineClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Candle>> GetHistoricalCandlesAsync(
        string symbol,
        string interval,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"https://fapi.binance.com/fapi/v1/klines?symbol={symbol}&interval={interval}&limit={limit}";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(json);

        var candles = new List<Candle>();

        foreach (var kline in document.RootElement.EnumerateArray())
        {
            candles.Add(new Candle
            {
                Time = kline[0].GetInt64(),
                Open = decimal.Parse(kline[1].GetString()!),
                High = decimal.Parse(kline[2].GetString()!),
                Low = decimal.Parse(kline[3].GetString()!),
                Close = decimal.Parse(kline[4].GetString()!),
                CloseTime = kline[6].GetInt64()
            });
        }

        return candles;
    }
}