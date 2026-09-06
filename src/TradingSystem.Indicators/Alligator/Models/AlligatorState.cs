using TradingSystem.Domain.MarketData;
using TradingSystem.Indicators.Alligator.Configuration;

namespace TradingSystem.Indicators.Alligator.Models;

public sealed class AlligatorState
{
    readonly AlligatorOptions _options;
    readonly Queue<MarketCandle> _queue = new();
    readonly SmoothedMovingAverage _jaw, _teeth, _lips;
    DateTime _lastCloseTimeUtc;

    public AlligatorState(AlligatorOptions options)
    {
        _options = options;
        _jaw = new(options.JawLength);
        _teeth = new(options.TeethLength);
        _lips = new(options.LipsLength);
    }

    public (decimal jaw, decimal teeth, decimal lips, decimal sma)? Add(MarketCandle candle)
    {
        if (candle.CloseTimeUtc <= _lastCloseTimeUtc)
            return null;

        _lastCloseTimeUtc = candle.CloseTimeUtc;
        _queue.Enqueue(candle);

        while (_queue.Count > _options.HistoryLimit)
            _queue.Dequeue();

        var h = (candle.High + candle.Low) / 2;
        var j = _jaw.Update(h);
        var t = _teeth.Update(h);
        var l = _lips.Update(h);

        if (_queue.Count < _options.SmaLength)
            return null;

        return (j, t, l, _queue.TakeLast(_options.SmaLength).Average(x => x.Close));
    }
}
