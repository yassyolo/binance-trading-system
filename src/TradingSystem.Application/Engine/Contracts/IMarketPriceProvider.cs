namespace TradingSystem.Application.Engine.Contracts;

public interface IMarketPriceProvider
{
    Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken ct);
}
