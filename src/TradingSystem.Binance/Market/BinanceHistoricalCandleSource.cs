using System.Globalization;
using System.Text.Json;
using TradingSystem.Application.MarketData;
using TradingSystem.Domain.MarketData;
namespace TradingSystem.Binance.Market;
public sealed class BinanceHistoricalCandleSource(HttpClient httpClient) : IHistoricalCandleSource
{
    public async Task<IReadOnlyList<MarketCandle>> LoadLatestAsync(string symbol,  string interval,  int limit,  CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 1500) throw new ArgumentOutOfRangeException(nameof(limit));
        using var response  =  await httpClient.GetAsync($"/fapi/v1/klines?symbol = {Uri.EscapeDataString(symbol)}&interval = {Uri.EscapeDataString(interval)}&limit = {limit}",  cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream  =  await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document  =  await JsonDocument.ParseAsync(stream,  cancellationToken: cancellationToken);
        return document.RootElement.EnumerateArray().Select(x  =>  new MarketCandle(
            symbol.ToUpperInvariant(),  interval.ToLowerInvariant(), 
            DateTimeOffset.FromUnixTimeMilliseconds(x[0].GetInt64()).UtcDateTime, 
            DateTimeOffset.FromUnixTimeMilliseconds(x[6].GetInt64()).UtcDateTime, 
            D(x[1]),  D(x[2]),  D(x[3]),  D(x[4]),  D(x[5]),  true)).OrderBy(x => x.OpenTimeUtc).ToArray();
    }
    private static decimal D(JsonElement x)  =>  decimal.Parse(x.GetString()!,  NumberStyles.Any,  CultureInfo.InvariantCulture);
}
