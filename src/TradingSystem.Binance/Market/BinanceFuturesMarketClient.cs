using System.Globalization;
using System.Text.Json;
using TradingSystem.Binance.Market.Contracts;

namespace TradingSystem.Binance.Market;

public sealed class BinanceFuturesMarketClient(HttpClient httpClient) : IBinanceFuturesMarketClient
{
    public async Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
       
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var url = $"fapi/v1/premiumIndex?symbol={Uri.EscapeDataString(normalizedSymbol)}";

        using var response = await httpClient.GetAsync(url, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new BinanceApiException(response.StatusCode, content, "load mark price");

        using var document = JsonDocument.Parse(content);
        var value = document.RootElement.GetProperty("markPrice").GetString();

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) && price > 0
            ? price
            : throw new InvalidOperationException($"Binance returned invalid mark price for '{normalizedSymbol}'.");
    }
}
