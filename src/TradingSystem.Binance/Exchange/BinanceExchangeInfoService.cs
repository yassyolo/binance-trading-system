using Microsoft.Extensions.Options;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Orders.Models;

namespace TradingSystem.Binance.Exchange;

public sealed class BinanceExchangeInfoService(
    IBinanceFuturesOrderClient ordersClient,
    IOptions<BinanceFuturesOptions> options)
{
    private sealed record CacheEntry(BinanceSymbolFilters Filters, DateTime ExpiresAtUtc);

    private readonly BinanceFuturesOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, CacheEntry> _filtersCache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<decimal> RoundPriceAsync(string symbol, decimal price, CancellationToken ct)
    {
        var filters = await GetFiltersAsync(symbol, ct);
       
        return QuantizeDown(price, filters.TickSize);
    }

    public async Task<decimal> RoundQuantityAsync(string symbol, decimal quantity, CancellationToken ct)
    {
        var filters = await GetFiltersAsync(symbol, ct);
        var rounded = QuantizeDown(quantity, filters.StepSize);
        
        return rounded < filters.MinQuantity ? 0 : rounded;
    }

    public async Task<BinanceSymbolFilters> GetFiltersAsync(string symbol, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        if (_filtersCache.TryGetValue(normalizedSymbol, out var cached) && cached.ExpiresAtUtc > now)
            return cached.Filters;

        await _gate.WaitAsync(ct);
       
        try
        {
            now = DateTime.UtcNow;
            if (_filtersCache.TryGetValue(normalizedSymbol, out cached) && cached.ExpiresAtUtc > now)
                return cached.Filters;

            var filters = await ordersClient.GetSymbolFiltersAsync(normalizedSymbol, ct);
           
            _filtersCache[normalizedSymbol] = new CacheEntry(filters, now.Add(_options.ExchangeInfoCacheDuration));

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
