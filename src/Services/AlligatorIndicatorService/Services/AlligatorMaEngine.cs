using System.Collections.Concurrent;
using AlligatorIndicatorService.Indicators;
using AlligatorIndicatorService.Models;

namespace AlligatorIndicatorService.Services;

public sealed class AlligatorMaEngine
{
    private const int HistoryLimit = 300;
    private const int SmaLength = 200;

    private readonly ConcurrentDictionary<string, List<Candle>> _candles = new();

    private readonly ConcurrentDictionary<string, AlligatorCalculator> _alligators = new();

    public void InitializeHistory(
        string symbol,
        string interval,
        IEnumerable<Candle> candles)
    {
        var key = $"{symbol}:{interval}";

        var list = candles.ToList();

        _candles[key] = list;

        var alligator = new AlligatorCalculator(
            jawLength: 13,
            teethLength: 8,
            lipsLength: 5);

        foreach (var candle in list)
        {
            alligator.Update(
                candle.High,
                candle.Low);
        }

        _alligators[key] = alligator;
    }

    public AlligatorMaPayload? Process(
        string symbol,
        string interval,
        Candle candle)
    {
        var key = $"{symbol}:{interval}";

        if (!_candles.TryGetValue(key, out var candles))
            return null;

        if (!_alligators.TryGetValue(key, out var alligator))
            return null;

        candles.Add(candle);

        if (candles.Count > HistoryLimit)
        {
            candles.RemoveAt(0);
        }

        var values = alligator.Update(
            candle.High,
            candle.Low);

        var closes = candles
            .Select(x => x.Close)
            .ToList();

        if (closes.Count < SmaLength)
            return null;

        var sma200 =
            closes.TakeLast(SmaLength).Average();

        return new AlligatorMaPayload
        {
            Symbol = symbol,
            Timeframe = interval,
            CandleCloseTime = candle.CloseTime,
            PublishedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Indicators = new AlligatorIndicators
            {
                AlligatorJaw = new IndicatorValue
                {
                    Value = values.Jaw
                },
                AlligatorTeeth = new IndicatorValue
                {
                    Value = values.Teeth
                },
                AlligatorLips = new IndicatorValue
                {
                    Value = values.Lips
                },
                Sma200 = new IndicatorValue
                {
                    Value = sma200
                }
            }
        };
    }
}