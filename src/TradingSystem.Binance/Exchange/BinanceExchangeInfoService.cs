using Microsoft.Extensions.Options;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;

namespace TradingSystem.Binance.Exchange;

public sealed class BinanceExchangeInfoService
{
    private sealed record CacheEntry(BinanceSymbolFilters Filters, DateTime ExpiresAtUtc);

    private readonly IBinanceFuturesOrderClient _orders;
    private readonly BinanceFuturesOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public BinanceExchangeInfoService(
        IBinanceFuturesOrderClient orders,
        IOptions<BinanceFuturesOptions> options)
    {
        _orders = orders;
        _options = options.Value;
    }

    public async Task<decimal> RoundPriceAsync(
        string symbol,
        decimal price,
        CancellationToken cancellationToken)
    {
        var filters = await GetFiltersAsync(symbol, cancellationToken);
        return QuantizeDown(price, filters.TickSize);
    }

    public async Task<decimal> RoundQuantityAsync(
        string symbol,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var filters = await GetFiltersAsync(symbol, cancellationToken);
        var rounded = QuantizeDown(quantity, filters.StepSize);
        return rounded < filters.MinQuantity ? 0 : rounded;
    }

    public async Task<BinanceSymbolFilters> GetFiltersAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        if (_cache.TryGetValue(normalizedSymbol, out var cached) && cached.ExpiresAtUtc > now)
            return cached.Filters;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            now = DateTime.UtcNow;
            if (_cache.TryGetValue(normalizedSymbol, out cached) && cached.ExpiresAtUtc > now)
                return cached.Filters;

            var filters = await _orders.GetSymbolFiltersAsync(normalizedSymbol, cancellationToken);
            _cache[normalizedSymbol] = new CacheEntry(
                filters,
                now.Add(_options.ExchangeInfoCacheDuration));

            return filters;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static decimal QuantizeDown(decimal value, decimal step)
        => step <= 0 ? value : Math.Floor(value / step) * step;
}
