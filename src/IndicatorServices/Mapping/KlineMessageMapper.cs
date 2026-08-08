using System.Globalization;
using TradingSystem.Contracts.Klines;
using TradingSystem.Domain.MarketData;

namespace IndicatorServices.Mapping;

internal static class KlineMessageMapper
{
    public static bool TryMap(ClosedKlineMessage m, out MarketCandle candle)
    {
        candle = default!;

        if (string.IsNullOrWhiteSpace(m.Symbol) ||
            string.IsNullOrWhiteSpace(m.Interval) ||
            m.Time <= 0 ||
            m.CloseTime < m.Time ||
            !TryParseDecimal(m.Open, out var open) ||
            !TryParseDecimal(m.High, out var high) ||
            !TryParseDecimal(m.Low, out var low) ||
            !TryParseDecimal(m.Close, out var close) ||
            !TryParseDecimal(m.Volume, out var volume))
        {
            return false;
        }

        if (high < low ||
            high < Math.Max(open, close) ||
            low > Math.Min(open, close) ||
            volume < 0)
        {
            return false;
        }

        candle = new MarketCandle(
            m.Symbol.Trim().ToUpperInvariant(),
            m.Interval.Trim().ToLowerInvariant(),
            DateTimeOffset.FromUnixTimeMilliseconds(m.Time).UtcDateTime,
            DateTimeOffset.FromUnixTimeMilliseconds(m.CloseTime).UtcDateTime,
            open,
            high,
            low,
            close,
            volume,
            true);

        return true;
    }

    private static bool TryParseDecimal(string? value, out decimal result) 
        => decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
}
