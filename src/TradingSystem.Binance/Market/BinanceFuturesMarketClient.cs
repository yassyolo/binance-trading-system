using System.Globalization;
using System.Text.Json;
using TradingSystem.Binance.Market.Contracts;

namespace TradingSystem.Binance.Market;

public sealed class BinanceFuturesMarketClient(HttpClient httpClient) : IBinanceFuturesMarketClient
{
    public async Task<decimal> GetMarkPriceAsync(string symbol,  CancellationToken cancellationToken  =  default)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol is required.",  nameof(symbol));
        using var response  =  await httpClient.GetAsync($"/fapi/v1/premiumIndex?symbol = {Uri.EscapeDataString(symbol.ToUpperInvariant())}",  cancellationToken);
        var content  =  await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new BinanceApiException(response.StatusCode,  content);
        using var document  =  JsonDocument.Parse(content);
        var value  =  document.RootElement.GetProperty("markPrice").GetString();
        return decimal.TryParse(value,  NumberStyles.Any,  CultureInfo.InvariantCulture,  out var price)  &&  price > 0
            ? price
            : throw new InvalidOperationException($"Binance returned invalid mark price for '{symbol}'.");
    }
}
