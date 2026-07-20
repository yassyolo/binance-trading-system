

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016MarketState
{
    private readonly object _sync  =  new();
    private Bot8016IndicatorSnapshot? _indicator;
    private readonly Dictionary<string,  Bot8016Candle> _latestCandles  = 
        new(StringComparer.OrdinalIgnoreCase);

    public void UpdateIndicator(Bot8016IndicatorSnapshot snapshot)
    {
        lock (_sync)
            _indicator  =  snapshot;
    }

    public void UpdateCandle(Bot8016Candle candle)
    {
        lock (_sync)
            _latestCandles[candle.Interval]  =  candle;
    }

    public Bot8016IndicatorSnapshot? GetIndicator()
    {
        lock (_sync)
            return _indicator;
    }

    public Bot8016Candle? GetLatestCandle(string interval)
    {
        lock (_sync)
            return _latestCandles.GetValueOrDefault(interval);
    }
}
