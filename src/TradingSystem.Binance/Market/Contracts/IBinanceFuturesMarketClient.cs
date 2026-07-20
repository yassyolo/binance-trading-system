namespace TradingSystem.Binance.Market.Contracts;

public interface IBinanceFuturesMarketClient
{
    Task<decimal> GetMarkPriceAsync(string symbol,  CancellationToken cancellationToken  =  default);
}
