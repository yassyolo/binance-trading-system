using Microsoft.Extensions.Options;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;

namespace TradingSystem.Binance.Exchange;

public sealed class BinanceExchangeInfoService
{
    private readonly IBinanceFuturesOrderClient _orders;
    private readonly BinanceFuturesOptions _options;
    private readonly SemaphoreSlim _gate  =  new(1,  1);
    private DateTime _expiresAtUtc;
    private readonly Dictionary<string,  BinanceSymbolFilters> _cache  =  new(StringComparer.OrdinalIgnoreCase);

    public BinanceExchangeInfoService(IBinanceFuturesOrderClient orders,  IOptions<BinanceFuturesOptions> options)
    {
        _orders  =  orders;
        _options  =  options.Value;
    }

    public async Task<decimal> RoundPriceAsync(string symbol,  decimal price,  CancellationToken cancellationToken)
    {
        var f  =  await GetFiltersAsync(symbol,  cancellationToken);
        return f.TickSize <= 0 ? price : Math.Floor(price / f.TickSize) * f.TickSize;
    }

    public async Task<decimal> RoundQuantityAsync(string symbol,  decimal quantity,  CancellationToken cancellationToken)
    {
        var f  =  await GetFiltersAsync(symbol,  cancellationToken);
        if (f.StepSize <= 0) return quantity;
        var rounded  =  Math.Floor(quantity / f.StepSize) * f.StepSize;
        return rounded < f.MinQuantity ? 0 : rounded;
    }

    public async Task<BinanceSymbolFilters> GetFiltersAsync(string symbol,  CancellationToken cancellationToken)
    {
        if (_expiresAtUtc > DateTime.UtcNow  &&  _cache.TryGetValue(symbol,  out var cached)) return cached;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_expiresAtUtc > DateTime.UtcNow  &&  _cache.TryGetValue(symbol,  out cached)) return cached;
            var filters  =  await _orders.GetSymbolFiltersAsync(symbol,  cancellationToken);
            _cache[symbol]  =  filters;
            _expiresAtUtc  =  DateTime.UtcNow.Add(_options.ExchangeInfoCacheDuration);
            return filters;
        }
        finally { _gate.Release(); }
    }
}
