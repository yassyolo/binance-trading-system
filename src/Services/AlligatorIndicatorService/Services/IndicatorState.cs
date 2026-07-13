using AlligatorIndicatorService.Indicators;
using AlligatorIndicatorService.Models;

namespace AlligatorIndicatorService.Services;

internal sealed class IndicatorState(
    int historyLimit,
    int smaLength,
    AlligatorCalculator alligator)
{
    private readonly Queue<Candle> _candles = new(historyLimit);
    private readonly object _sync = new();
    private long _lastCloseTime;

    public void Initialize(IEnumerable<Candle> candles)
    {
        lock (_sync)
        {
            _candles.Clear();
            _lastCloseTime = 0;

            foreach (var candle in candles
                         .Where(x => x.IsClosed)
                         .OrderBy(x => x.CloseTime)
                         .TakeLast(historyLimit))
            {
                _candles.Enqueue(candle);
                alligator.Update(candle.High, candle.Low);
                _lastCloseTime = candle.CloseTime;
            }
        }
    }

    public IndicatorUpdateResult? Update(Candle candle)
    {
        lock (_sync)
        {
            if (!candle.IsClosed || candle.CloseTime <= _lastCloseTime)
                return null;

            _lastCloseTime = candle.CloseTime;
            _candles.Enqueue(candle);

            while (_candles.Count > historyLimit)
                _candles.Dequeue();

            var values = alligator.Update(candle.High, candle.Low);

            if (_candles.Count < smaLength)
                return null;

            var sma = _candles
                .TakeLast(smaLength)
                .Average(x => x.Close);

            return new IndicatorUpdateResult(values, sma);
        }
    }
}

internal sealed record IndicatorUpdateResult(
    AlligatorValues Values,
    decimal Sma);
