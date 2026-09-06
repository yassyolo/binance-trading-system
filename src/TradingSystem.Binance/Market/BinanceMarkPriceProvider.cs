using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Binance.Market.Contracts;

namespace TradingSystem.Binance.Market;

public sealed class BinanceMarkPriceProvider(
    IBinanceFuturesMarketClient marketClient) 
    : IMarketPriceProvider
{
    public Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken ct)  
        => marketClient.GetMarkPriceAsync(symbol, ct);
}
