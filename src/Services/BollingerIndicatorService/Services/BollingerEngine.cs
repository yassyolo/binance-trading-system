using System.Collections.Concurrent;
using BollingerIndicatorService.Configuration;
using BollingerIndicatorService.Models;
using Microsoft.Extensions.Options;

namespace BollingerIndicatorService.Services;

public sealed class BollingerEngine(IOptions<BollingerOptions> options)
{
    private readonly BollingerOptions options = options.Value;
    private readonly ConcurrentDictionary<string, BollingerIndicatorState> _states = new();

    public BollingerPayload? InitializeHistory(string symbol, string interval, IEnumerable<BollingerCandle> candles)
    {
        var key = BuildKey(symbol, interval);

        var state = new BollingerIndicatorState(options.HistoryLimit, options.Bands);

        _states[key] = state;

        return state.Initialize(symbol, interval, candles);
    }

    public BollingerPayload? Process(string symbol, string interval, BollingerCandle candle)
    {
        var key = BuildKey(symbol, interval);

        if (!_states.TryGetValue(key, out var state))
            return null;

        return state.Update(symbol, interval, candle);
    }

    private static string BuildKey(string symbol, string interval)
        => $"{symbol.ToUpperInvariant()}:{interval.ToLowerInvariant()}";
}