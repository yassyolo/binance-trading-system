using System.Globalization;
using System.Text.Json;
using TradingSystem.Binance.Market.Contracts;

namespace TradingSystem.Binance.Market;

public sealed class BinanceFuturesMarketClient(
    HttpClient httpClient) 
    : IBinanceFuturesMarketClient
{
    public async Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/fapi/v1/premiumIndex?symbol={symbol}", cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(json);

        var value = document.RootElement.GetProperty("markPrice").GetString();

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Binance returned empty mark price for {symbol}.");

        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}