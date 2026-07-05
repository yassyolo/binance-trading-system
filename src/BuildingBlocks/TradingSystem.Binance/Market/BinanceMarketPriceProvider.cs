using TradingSystem.Application.Engine;
using TradingSystem.Binance.Market.Contracts;

namespace TradingSystem.Binance.Market;

public sealed class BinanceMarketPriceProvider(IBinanceFuturesMarketClient client) : IMarketPriceProvider
{
    public Task<decimal> GetMarkPriceAsync(
        string symbol,
        CancellationToken cancellationToken)
        => client.GetMarkPriceAsync(symbol, cancellationToken);
}