namespace TradingSystem.Application.Engine;

public interface IMarketPriceProvider
{
    Task<decimal> GetMarkPriceAsync(string symbol,  CancellationToken ct);
}
