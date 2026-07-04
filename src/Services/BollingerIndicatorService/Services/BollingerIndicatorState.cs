using BollingerIndicatorService.Configuration;
using BollingerIndicatorService.Indicators;
using BollingerIndicatorService.Models;

namespace BollingerIndicatorService.Services;

internal sealed class BollingerIndicatorState(
    int historyLimit,
    IReadOnlyCollection<BollingerBandOptions> bands)
{
    private readonly List<BollingerCandle> _candles = [];
    private readonly object _lock = new();

    private BollingerPayload? _lastPayload;

    public BollingerPayload? Initialize(string symbol, string interval, IEnumerable<BollingerCandle> candles)
    {
        lock (_lock)
        {
            _candles.Clear();

            _candles.AddRange(candles.OrderBy(x => x.Time).TakeLast(historyLimit));

            _lastPayload = Compute(symbol, interval);

            return _lastPayload;
        }
    }

    public BollingerPayload? Update(string symbol, string interval, BollingerCandle candle)
    {
        lock (_lock)
        {
            var existingIndex = _candles.FindIndex(x => x.CloseTime == candle.CloseTime);

            if (existingIndex >= 0)
            {
                if (_candles[existingIndex] == candle)
                    return null;

                _candles[existingIndex] = candle;
            }
            else
            {
                _candles.Add(candle);
            }

            _candles.Sort((a, b) => a.Time.CompareTo(b.Time));

            if (_candles.Count > historyLimit)
                _candles.RemoveRange(0, _candles.Count - historyLimit);

            var payload = Compute(symbol, interval);

            if (payload is not null)
                _lastPayload = payload;

            return payload;
        }
    }

    private BollingerPayload? Compute(string symbol, string interval)
    {
        if (_candles.Count == 0)
            return null;

        var current = _candles[^1];
        var indicators = new Dictionary<string, BollingerIndicatorValue>();

        foreach (var band in bands)
        {
            if (!band.MaType.Equals("SMA", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{band.Name}: only SMA is supported.");

            var sourceValues = _candles.Select(x => GetSource(x, band.Source));

            var result = BollingerCalculator.Calculate(sourceValues, band.Length, band.Mult);

            if (result is null)
                return null;

            BollingerIndicatorValue? previous = null;
            _lastPayload?.Indicators.TryGetValue(band.Name, out previous);

            indicators[band.Name] = new BollingerIndicatorValue
            {
                Length = band.Length,
                Source = band.Source,
                MaType = band.MaType,
                Mult = band.Mult,

                BasisCurrent = result.Basis,
                BasisPrev = previous?.BasisCurrent,

                UpperCurrent = result.Upper,
                UpperPrev = previous?.UpperCurrent,

                LowerCurrent = result.Lower,
                LowerPrev = previous?.LowerCurrent
            };
        }

        return new BollingerPayload
        {
            Symbol = symbol.ToUpperInvariant(),
            Timeframe = interval.ToLowerInvariant(),
            CandleOpenTime = current.Time,
            CandleCloseTime = current.CloseTime,
            PublishedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Indicators = indicators
        };
    }

    private static decimal GetSource(BollingerCandle candle, string source)
    {
        return source.ToLowerInvariant() switch
        {
            "open" => candle.Open,
            "close" => candle.Close,
            _ => throw new InvalidOperationException($"Unsupported Bollinger source: {source}")
        };
    }
}