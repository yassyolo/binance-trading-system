using System.Collections.Concurrent;
using AlligatorIndicatorService.Configuration;
using AlligatorIndicatorService.Indicators;
using AlligatorIndicatorService.Models;
using Microsoft.Extensions.Options;

namespace AlligatorIndicatorService.Services;

public sealed class AlligatorMaEngine(IOptions<AlligatorOptions> options)
{
    private readonly AlligatorOptions options = options.Value;
    private readonly ConcurrentDictionary<string, IndicatorState> _states = new();

    public void InitializeHistory(string symbol, string interval, IEnumerable<Candle> candles)
    {
        var key = CreateKey(symbol, interval);

        var state = CreateState();
        state.Initialize(candles);

        _states[key] = state;
    }

    public AlligatorMaPayload? Process(string symbol, string interval, Candle candle)
    {
        var key = CreateKey(symbol, interval);

        if (!_states.TryGetValue(key, out var state))
            return null;

        var result = state.Update(candle);

        if (result.Sma is null)
            return null;

        return new AlligatorMaPayload
        {
            Symbol = symbol.ToUpperInvariant(),
            Timeframe = interval.ToLowerInvariant(),
            CandleCloseTime = candle.CloseTime,
            PublishedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Indicators = new AlligatorIndicators
            {
                AlligatorJaw = new IndicatorValue { Value = result.Values.Jaw },
                AlligatorTeeth = new IndicatorValue { Value = result.Values.Teeth },
                AlligatorLips = new IndicatorValue { Value = result.Values.Lips },
                Sma200 = new IndicatorValue { Value = result.Sma.Value }
            }
        };
    }

    private IndicatorState CreateState()
    {
        var calculator = new AlligatorCalculator(
            options.JawLength,
            options.TeethLength,
            options.LipsLength);

        return new IndicatorState(
            options.HistoryLimit,
            options.SmaLength,
            calculator);
    }

    private static string CreateKey(string symbol, string interval)
        => $"{symbol.ToUpperInvariant()}:{interval.ToLowerInvariant()}";
}