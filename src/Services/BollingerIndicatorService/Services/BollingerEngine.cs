using System.Collections.Concurrent;
using BollingerIndicatorService.Indicators;
using BollingerIndicatorService.Models;

namespace BollingerIndicatorService.Services;

public sealed class BollingerEngine
{
    private readonly List<BollingerDefinition> _definitions;
    private readonly int _historyLimit;

    private readonly ConcurrentDictionary<string, List<BollingerCandle>> _candles = new();
    private readonly ConcurrentDictionary<string, BollingerPayload> _lastState = new();

    public BollingerEngine(IConfiguration configuration)
    {
        _definitions =
        [
            new BollingerDefinition(
                Name: configuration["Bollinger:Bands:0:Name"] ?? "BB4_OPEN",
                Length: configuration.GetValue<int>("Bollinger:Bands:0:Length", 4),
                Source: configuration["Bollinger:Bands:0:Source"] ?? "open",
                Mult: configuration.GetValue<decimal>("Bollinger:Bands:0:Mult", 4),
                MaType: configuration["Bollinger:Bands:0:MaType"] ?? "SMA"),

            new BollingerDefinition(
                Name: configuration["Bollinger:Bands:1:Name"] ?? "BB20_CLOSE",
                Length: configuration.GetValue<int>("Bollinger:Bands:1:Length", 20),
                Source: configuration["Bollinger:Bands:1:Source"] ?? "close",
                Mult: configuration.GetValue<decimal>("Bollinger:Bands:1:Mult", 2),
                MaType: configuration["Bollinger:Bands:1:MaType"] ?? "SMA")
        ];

        _historyLimit = configuration.GetValue<int>("Bollinger:HistoryLimit", 200);
    }

    public void InitializeHistory(string symbol, string interval, IEnumerable<BollingerCandle> candles)
    {
        var key = BuildKey(symbol, interval);

        _candles[key] = candles
            .OrderBy(x => x.Time)
            .TakeLast(_historyLimit)
            .ToList();

        var payload = Compute(symbol, interval);

        if (payload is not null)
        {
            _lastState[key] = payload;
        }
    }

    public BollingerPayload? Process(string symbol, string interval, BollingerCandle candle)
    {
        var key = BuildKey(symbol, interval);

        if (!_candles.TryGetValue(key, out var candles))
        {
            candles = [];
            _candles[key] = candles;
        }

        var existingIndex = candles.FindIndex(x => x.CloseTime == candle.CloseTime);

        if (existingIndex >= 0)
        {
            if (candles[existingIndex] == candle)
                return null;

            candles[existingIndex] = candle;
        }
        else
        {
            candles.Add(candle);
        }

        candles.Sort((a, b) => a.Time.CompareTo(b.Time));

        if (candles.Count > _historyLimit)
        {
            candles.RemoveRange(0, candles.Count - _historyLimit);
        }

        var payload = Compute(symbol, interval);

        if (payload is not null)
        {
            _lastState[key] = payload;
        }

        return payload;
    }

    private BollingerPayload? Compute(string symbol, string interval)
    {
        var key = BuildKey(symbol, interval);

        if (!_candles.TryGetValue(key, out var candles) || candles.Count == 0)
            return null;

        var current = candles[^1];

        _lastState.TryGetValue(key, out var previousPayload);

        var indicators = new Dictionary<string, BollingerIndicatorValue>();

        foreach (var definition in _definitions)
        {
            if (!definition.MaType.Equals("SMA", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{definition.Name}: only SMA is supported.");

            var sourceValues = candles
                .Select(c => GetSource(c, definition.Source))
                .ToList();

            var result = BollingerCalculator.Calculate(
                sourceValues,
                definition.Length,
                definition.Mult);

            if (result is null)
                return null;

            BollingerIndicatorValue? previousIndicator = null;

            if (previousPayload is not null)
            {
                previousPayload.Indicators.TryGetValue(
                    definition.Name,
                    out previousIndicator);
            }

            indicators[definition.Name] = new BollingerIndicatorValue
            {
                Length = definition.Length,
                Source = definition.Source,
                MaType = definition.MaType,
                Mult = definition.Mult,

                BasisCurrent = result.Basis,
                BasisPrev = previousIndicator?.BasisCurrent,

                UpperCurrent = result.Upper,
                UpperPrev = previousIndicator?.UpperCurrent,

                LowerCurrent = result.Lower,
                LowerPrev = previousIndicator?.LowerCurrent
            };
        }

        return new BollingerPayload
        {
            Symbol = symbol,
            Timeframe = interval,
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

    private static string BuildKey(string symbol, string interval)
        => $"{symbol.ToUpperInvariant()}:{interval.ToLowerInvariant()}";
}