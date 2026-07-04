using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Binance.Configuration;

namespace TradingSystem.Binance.Market;

public sealed class BinanceFuturesMarketClient : IBinanceFuturesMarketClient
{
    private readonly HttpClient _httpClient;
    private readonly BinanceFuturesOptions _options;

    public BinanceFuturesMarketClient(
        HttpClient httpClient,
        IOptions<BinanceFuturesOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<decimal> GetMarkPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"/fapi/v1/premiumIndex?symbol={symbol}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(json);

        var value = document.RootElement
            .GetProperty("markPrice")
            .GetString();

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Binance returned empty mark price for {symbol}.");

        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}