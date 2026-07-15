using System.Globalization;
using System.Text.Json;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.HistoricalData;

public sealed class BinanceFuturesHistoricalCandleSource(HttpClient httpClient) : IHistoricalCandleSource
{
    private const int Limit = 1500;

    public async Task<IReadOnlyList<HistoricalCandle>> LoadAsync(string symbol, string interval, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTimeOffset(fromUtc ?? DateTime.UtcNow.AddMonths(-3)).ToUnixTimeMilliseconds();
        var end = new DateTimeOffset(toUtc ?? DateTime.UtcNow).ToUnixTimeMilliseconds();
        var result = new List<HistoricalCandle>();
        while (start < end)
        {
            var url = $"https://fapi.binance.com/fapi/v1/klines?symbol={Uri.EscapeDataString(symbol)}&interval={Uri.EscapeDataString(interval)}&startTime={start}&endTime={end}&limit={Limit}";
            using var response = await httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Binance historical request failed: {(int)response.StatusCode} {body}");
            using var document = JsonDocument.Parse(body);
            var batch = document.RootElement.EnumerateArray().ToArray();
            if (batch.Length == 0) break;
            foreach (var k in batch)
            {
                var values = k.EnumerateArray().ToArray();
                var openMs = values[0].GetInt64(); var closeMs = values[6].GetInt64();
                result.Add(new HistoricalCandle
                {
                    Symbol = symbol,
                    Interval = interval,
                    OpenTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(openMs).UtcDateTime,
                    CloseTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(closeMs).UtcDateTime,
                    Open = D(values[1]),
                    High = D(values[2]),
                    Low = D(values[3]),
                    Close = D(values[4]),
                    Volume = D(values[5])
                });
            }
            var next = batch[^1].EnumerateArray().First().GetInt64() + 1;
            if (next <= start || batch.Length < Limit) break;
            start = next;
        }
        return result.OrderBy(x => x.OpenTimeUtc).GroupBy(x => x.OpenTimeUtc).Select(x => x.Last()).ToArray();
    }
    private static decimal D(JsonElement value) => decimal.Parse(value.GetString()!, NumberStyles.Any, CultureInfo.InvariantCulture);
}
