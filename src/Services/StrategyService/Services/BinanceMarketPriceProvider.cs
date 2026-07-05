using TradingSystem.Application.Engine;
using TradingSystem.Binance.Market;

namespace StrategyService.Services;

public sealed class BinanceMarketPriceProvider : IMarketPriceProvider
{
    private readonly IBinanceFuturesMarketClient _client;

    public BinanceMarketPriceProvider(IBinanceFuturesMarketClient client)
    {
        _client = client;
    }

    public Task<decimal> GetMarkPriceAsync(
        string symbol,
        CancellationToken cancellationToken)
        => _client.GetMarkPriceAsync(symbol, cancellationToken);
}