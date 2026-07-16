using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Bot8011.Models;

namespace TradingSystem.Backtesting.Bot8011.Signals;

// Smoke-test signal source only. BOT8011 itself receives external JSON signals.
public sealed class EmaCrossDemoSignalSource(int fastPeriod = 20, int slowPeriod = 50) : IBot8011SignalSource
{
    public Task<IReadOnlyList<Bot8011Signal>> LoadAsync(IReadOnlyList<HistoricalCandle> candles, CancellationToken cancellationToken = default)
    {
        var signals = new List<Bot8011Signal>();
        decimal? fast = null, slow = null;
        decimal? previousFast = null, previousSlow = null;
        var fastAlpha = 2m / (fastPeriod + 1m);
        var slowAlpha = 2m / (slowPeriod + 1m);
        foreach (var candle in candles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            fast = fast is null ? candle.Close : fast + fastAlpha * (candle.Close - fast.Value);
            slow = slow is null ? candle.Close : slow + slowAlpha * (candle.Close - slow.Value);
            if (previousFast.HasValue && previousSlow.HasValue)
            {
                if (previousFast <= previousSlow && fast > slow) signals.Add(new(candle.CloseTimeUtc, TradeSide.Long, "EMA_DEMO"));
                else if (previousFast >= previousSlow && fast < slow) signals.Add(new(candle.CloseTimeUtc, TradeSide.Short, "EMA_DEMO"));
            }
            previousFast = fast; previousSlow = slow;
        }
        return Task.FromResult<IReadOnlyList<Bot8011Signal>>(signals);
    }
}
