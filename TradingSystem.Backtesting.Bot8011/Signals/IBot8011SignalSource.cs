using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Bot8011.Models;

namespace TradingSystem.Backtesting.Bot8011.Signals;

public interface IBot8011SignalSource
{
    Task<IReadOnlyList<Bot8011Signal>> LoadAsync(IReadOnlyList<HistoricalCandle> candles, CancellationToken cancellationToken = default);
}
