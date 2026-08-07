using TradingSystem.Domain.MarketData;

namespace TradingSystem.Application.MarketData;

public interface IHistoricalCandleSource
{
    Task<IReadOnlyList<MarketCandle>> LoadLatestAsync(string symbol, string interval, int limit, CancellationToken ct);
}
