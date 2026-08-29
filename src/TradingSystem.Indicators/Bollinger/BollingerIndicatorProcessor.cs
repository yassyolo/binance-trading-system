using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TradingSystem.Contracts.Indicators;
using TradingSystem.Domain.MarketData;
using TradingSystem.Indicators.Bollinger.Configuration;
using TradingSystem.Indicators.Bollinger.Models;
using TradingSystem.Indicators.Contracts;

namespace TradingSystem.Indicators.Bollinger;

public sealed class BollingerIndicatorProcessor(IOptions<BollingerOptions> options) : IIndicatorProcessor
{
    private readonly BollingerOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, BollingerState> _states = new();

    public string Name => "bb";
    public IReadOnlyCollection<string> Symbols => _options.Symbols;
    public IReadOnlyCollection<string> Intervals => _options.Intervals;
    public int RequiredHistory => Math.Max(_options.HistoryLimit, _options.Bands.Max(x => x.Length));

    public void Initialize(string symbol, string interval, IReadOnlyList<MarketCandle> candles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(interval);
        ArgumentNullException.ThrowIfNull(candles);

        var state = new BollingerState(_options);
        foreach (var candle in candles.Where(x => x.IsClosed)
                     .OrderBy(x => x.CloseTimeUtc)
                     .TakeLast(_options.HistoryLimit))
        {
            state.Add(candle);
        }

        _states[Key(symbol, interval)] = state;
    }

    public IndicatorSnapshotMessage? Process(MarketCandle candle, DateTimeOffset publishedAt)
    {
        if (!candle.IsClosed || !_states.TryGetValue(Key(candle.Symbol, candle.Interval), out var state))
            return null;

        var values = state.Add(candle);
        if (values is null)
            return null;

        return new IndicatorSnapshotMessage
        {
            Type = Name,
            Symbol = candle.Symbol,
            Timeframe = candle.Interval,
            CandleOpenTime = new DateTimeOffset(candle.OpenTimeUtc).ToUnixTimeMilliseconds(),
            CandleCloseTime = new DateTimeOffset(candle.CloseTimeUtc).ToUnixTimeMilliseconds(),
            PublishedAt = publishedAt.ToUnixTimeMilliseconds(),
            Indicators = values
        };
    }

    private static string Key(string symbol, string interval) =>
        $"{symbol.ToUpperInvariant()}:{interval.ToLowerInvariant()}";  
}
