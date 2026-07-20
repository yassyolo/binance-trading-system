using TradingSystem.Domain.MarketData;

namespace TradingSystem.Application.MarketData;

public interface IHistoricalCandleRangeSource
{
    Task<IReadOnlyList<MarketCandle>> LoadAsync(
        string symbol, 
        string interval, 
        DateTime? fromUtc, 
        DateTime? toUtc, 
        CancellationToken cancellationToken  =  default);
}
