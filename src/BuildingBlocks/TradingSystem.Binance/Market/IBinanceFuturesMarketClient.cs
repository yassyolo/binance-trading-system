namespace TradingSystem.Binance.Market;

public interface IBinanceFuturesMarketClient
{
    Task<decimal> GetMarkPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default);
}