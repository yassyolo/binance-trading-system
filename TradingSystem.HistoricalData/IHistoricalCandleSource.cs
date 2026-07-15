using TradingSystem.Backtesting.Models;

namespace TradingSystem.HistoricalData;

public interface IHistoricalCandleSource
{
    Task<IReadOnlyList<HistoricalCandle>> LoadAsync(string symbol, string interval, DateTime? fromUtc, DateTime? toUtc,
        CancellationToken cancellationToken = default);
}
