using TradingSystem.Contracts.Indicators;
using TradingSystem.Domain.MarketData;
using TradingSystem.Indicators.Bollinger.Configuration;
using TradingSystem.Indicators.Common;

namespace TradingSystem.Indicators.Bollinger.Models;

public sealed class BollingerState
{
    private readonly BollingerOptions _options;
    private readonly List<MarketCandle> _candles = [];
    private DateTime _lastCloseTimeUtc;
    private Dictionary<string, (decimal Basis, decimal Upper, decimal Lower)> _previous = [];

    public BollingerState(BollingerOptions options) => _options = options;

    public IReadOnlyDictionary<string, IndicatorValueMessage>? Add(MarketCandle candle)
    {
        if (!candle.IsClosed || candle.CloseTimeUtc <= _lastCloseTimeUtc)
            return null;

        _lastCloseTimeUtc = candle.CloseTimeUtc;
        _candles.Add(candle);
       
        if (_candles.Count > _options.HistoryLimit)
            _candles.RemoveRange(0, _candles.Count - _options.HistoryLimit);

        var result = new Dictionary<string, IndicatorValueMessage>(StringComparer.OrdinalIgnoreCase);
        var current = new Dictionary<string, (decimal Basis, decimal Upper, decimal Lower)>(StringComparer.OrdinalIgnoreCase);

        foreach (var band in _options.Bands)
        {
            var values = _candles.TakeLast(band.Length)
                .Select(x => band.Source.Equals("open", StringComparison.OrdinalIgnoreCase) ? x.Open : x.Close)
                .ToArray();

            if (values.Length < band.Length)
                return null;

            var basis = values.Average();
            var deviation = band.Multiplier * IndicatorMath.StandardDeviation(values);
            var upper = basis + deviation;
            var lower = basis - deviation;
            _previous.TryGetValue(band.Name, out var previous);

            result[$"{band.Name}.basis"] = new IndicatorValueMessage { Value = basis, PreviousValue = previous.Basis, Metadata = Metadata(band) };
            result[$"{band.Name}.upper"] = new IndicatorValueMessage { Value = upper, PreviousValue = previous.Upper, Metadata = Metadata(band) };
            result[$"{band.Name}.lower"] = new IndicatorValueMessage { Value = lower, PreviousValue = previous.Lower, Metadata = Metadata(band) };
            current[band.Name] = (basis, upper, lower);
        }

        _previous = current;
        return result;
    }

    private static IReadOnlyDictionary<string, string> Metadata(BollingerBandOptions band) =>
        new Dictionary<string, string>
        {
            ["length"] = band.Length.ToString(),
            ["source"] = band.Source,
            ["ma_type"] = "SMA",
            ["multiplier"] = band.Multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
}
