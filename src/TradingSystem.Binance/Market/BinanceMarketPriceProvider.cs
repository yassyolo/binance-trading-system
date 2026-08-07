using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Binance.Market.Contracts;

namespace TradingSystem.Binance.Market;

public sealed class BinanceMarketPriceProvider(
    IBinanceFuturesMarketClient client) 
    : IMarketPriceProvider
{
    public Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken ct)  
        => client.GetMarkPriceAsync(symbol, ct);
}
