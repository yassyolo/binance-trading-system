using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Models;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Signals;

public sealed class EmaCrossDemoSignalSource
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;

    public EmaCrossDemoSignalSource(int fastPeriod = 20, int slowPeriod = 50)
    {
        if (fastPeriod <= 0)
            throw new ArgumentOutOfRangeException(nameof(fastPeriod));
        if (slowPeriod <= fastPeriod)
            throw new ArgumentOutOfRangeException(nameof(slowPeriod), "Slow period must be greater than fast period.");

        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
    }

    public IReadOnlyList<HistoricalBotSignal> Generate(IReadOnlyList<MarketCandle> candles)
    {
        ArgumentNullException.ThrowIfNull(candles);

        var ordered = candles
            .Where(x => x.IsClosed)
            .OrderBy(x => x.OpenTimeUtc)
            .ToArray();

        if (ordered.Length < _slowPeriod)
            return [];

        var result = new List<HistoricalBotSignal>();
        decimal? fast = null;
        decimal? slow = null;
        decimal? previousFast = null;
        decimal? previousSlow = null;
        var fastAlpha = 2m / (_fastPeriod + 1m);
        var slowAlpha = 2m / (_slowPeriod + 1m);

        for (var index = 0; index < ordered.Length; index++)
        {
            var candle = ordered[index];
            fast = fast is null ? candle.Close : fast + fastAlpha * (candle.Close - fast.Value);
            slow = slow is null ? candle.Close : slow + slowAlpha * (candle.Close - slow.Value);

            if (index >= _slowPeriod - 1 && previousFast.HasValue && previousSlow.HasValue)
            {
                if (previousFast <= previousSlow && fast > slow)
                    result.Add(new(candle.CloseTimeUtc, TradeSide.Long, "EMA_DEMO"));
                else if (previousFast >= previousSlow && fast < slow)
                    result.Add(new(candle.CloseTimeUtc, TradeSide.Short, "EMA_DEMO"));
            }

            previousFast = fast;
            previousSlow = slow;
        }

        return result;
    }
}
