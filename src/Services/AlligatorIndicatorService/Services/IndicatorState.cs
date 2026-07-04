using AlligatorIndicatorService.Indicators;
using AlligatorIndicatorService.Models;

namespace AlligatorIndicatorService.Services;

internal sealed class IndicatorState(
    int historyLimit,
    int smaLength,
    AlligatorCalculator alligator)
{
    private Queue<Candle> _candles = new(historyLimit);
    private readonly object _lock = new();

    public void Initialize(IEnumerable<Candle> candles)
    {
        lock (_lock)
        {
            _candles.Clear();

            foreach (var candle in candles.TakeLast(historyLimit))
            {
                _candles.Enqueue(candle);
                alligator.Update(candle.High, candle.Low);
            }
        }
    }

    public (AlligatorValues Values, decimal? Sma) Update(Candle candle)
    {
        lock (_lock)
        {
            _candles.Enqueue(candle);

            while (_candles.Count > historyLimit)
                _candles.Dequeue();

            var values = alligator.Update(candle.High, candle.Low);

            if (_candles.Count < smaLength)
                return (values, null);

            var sma = _candles.TakeLast(smaLength).Average(x => x.Close);

            return (values, sma);
        }
    }
}