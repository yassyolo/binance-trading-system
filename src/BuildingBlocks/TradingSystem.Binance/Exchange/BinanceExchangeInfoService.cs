using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Exchange;

namespace StrategyService.Services;

public sealed class BinanceExchangeInfoService
{
    private readonly HttpClient _httpClient;
    private readonly BinanceFuturesOptions options;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private DateTime _expiresAtUtc;
    private Dictionary<string, BinanceSymbolTradingRules> _rules = new();

    public BinanceExchangeInfoService(
        HttpClient httpClient,
        IOptions<BinanceFuturesOptions> _options)
    {
        _httpClient = httpClient;
        options = _options.Value;
        _httpClient.BaseAddress = new Uri(options.BaseUrl);
    }

    public async Task<decimal> RoundPriceAsync(string symbol, decimal price,  CancellationToken cancellationToken)
    {
        var rules = await GetRulesAsync(symbol, cancellationToken);

        return rules.TickSize <= 0
            ? price
            : Math.Floor(price / rules.TickSize) * rules.TickSize;
    }

    public async Task<decimal> RoundQuantityAsync(string symbol, decimal quantity, CancellationToken cancellationToken)
    {
        var rules = await GetRulesAsync(symbol, cancellationToken);

        if (rules.StepSize <= 0)
            return quantity;

        var rounded = Math.Floor(quantity / rules.StepSize) * rules.StepSize;

        return rounded < rules.MinQuantity
            ? 0
            : rounded;
    }

    private async Task<BinanceSymbolTradingRules> GetRulesAsync(string symbol, CancellationToken cancellationToken)
    {
        await EnsureCacheAsync(cancellationToken);

        return _rules.TryGetValue(symbol.ToUpperInvariant(), out var rules)
            ? rules
            : new BinanceSymbolTradingRules(0.01m, 0.001m, 0.001m);
    }

    private async Task EnsureCacheAsync(CancellationToken cancellationToken)
    {
        if (_expiresAtUtc > DateTime.UtcNow && _rules.Count > 0)
            return;

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_expiresAtUtc > DateTime.UtcNow && _rules.Count > 0)
                return;

            using var response = await _httpClient.GetAsync("/fapi/v1/exchangeInfo", cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var result = new Dictionary<string, BinanceSymbolTradingRules>();

            foreach (var symbolElement in document.RootElement.GetProperty("symbols").EnumerateArray())
            {
                var symbolName = symbolElement.GetProperty("symbol").GetString();

                if (string.IsNullOrWhiteSpace(symbolName))
                    continue;

                decimal tickSize = 0;
                decimal stepSize = 0;
                decimal minQuantity = 0;

                foreach (var filter in symbolElement.GetProperty("filters").EnumerateArray())
                {
                    var type = filter.GetProperty("filterType").GetString();

                    if (type == "PRICE_FILTER")
                    {
                        tickSize = ParseDecimal(filter.GetProperty("tickSize").GetString());
                    }

                    if (type == "LOT_SIZE")
                    {
                        stepSize = ParseDecimal(filter.GetProperty("stepSize").GetString());
                        minQuantity = ParseDecimal(filter.GetProperty("minQty").GetString());
                    }
                }

                result[symbolName.ToUpperInvariant()] = new BinanceSymbolTradingRules(tickSize, stepSize, minQuantity);
            }

            _rules = result;
            _expiresAtUtc = DateTime.UtcNow.AddHours(1);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static decimal ParseDecimal(string? value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed : 0;
}