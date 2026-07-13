using System.Collections.Concurrent;
using AlligatorIndicatorService.Configuration;
using AlligatorIndicatorService.Indicators;
using AlligatorIndicatorService.Models;
using Microsoft.Extensions.Options;

namespace AlligatorIndicatorService.Services;

public sealed class AlligatorMaEngine(IOptions<AlligatorOptions> options)
{
    private readonly AlligatorOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, IndicatorState> _states = new();

    public void InitializeHistory(
        string symbol,
        string interval,
        IEnumerable<Candle> candles)
    {
        var state = CreateState();
        state.Initialize(candles);

        _states[CreateKey(symbol, interval)] = state;
    }

    public AlligatorMaPayload? Process(
        string symbol,
        string interval,
        Candle candle)
    {
        if (_options.PublishOnlyClosedCandles && !candle.IsClosed)
            return null;

        if (!_states.TryGetValue(CreateKey(symbol, interval), out var state))
            return null;

        var result = state.Update(candle);
        if (result is null)
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
                Sma200 = new IndicatorValue { Value = result.Sma }
            }
        };
    }

    private IndicatorState CreateState()
        => new(
            _options.HistoryLimit,
            _options.SmaLength,
            new AlligatorCalculator(
                _options.JawLength,
                _options.TeethLength,
                _options.LipsLength));

    private static string CreateKey(string symbol, string interval)
        => $"{symbol.ToUpperInvariant()}:{interval.ToLowerInvariant()}";
}
