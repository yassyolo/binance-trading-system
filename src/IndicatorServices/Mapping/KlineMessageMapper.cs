using System.Globalization;
using TradingSystem.Contracts.Klines;
using TradingSystem.Domain.MarketData;

namespace IndicatorServices.Mapping;

internal static class KlineMessageMapper
{
    public static bool TryMap(ClosedKlineMessage message, out MarketCandle candle)
    {
        candle = default!;

        if (string.IsNullOrWhiteSpace(message.Symbol) ||
            string.IsNullOrWhiteSpace(message.Interval) ||
            message.Time <= 0 ||
            message.CloseTime < message.Time ||
            !TryParseDecimal(message.Open, out var open) ||
            !TryParseDecimal(message.High, out var high) ||
            !TryParseDecimal(message.Low, out var low) ||
            !TryParseDecimal(message.Close, out var close) ||
            !TryParseDecimal(message.Volume, out var volume))
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
            message.Symbol.Trim().ToUpperInvariant(),
            message.Interval.Trim().ToLowerInvariant(),
            DateTimeOffset.FromUnixTimeMilliseconds(message.Time).UtcDateTime,
            DateTimeOffset.FromUnixTimeMilliseconds(message.CloseTime).UtcDateTime,
            open,
            high,
            low,
            close,
            volume,
            true);

        return true;
    }

    private static bool TryParseDecimal(string? value, out decimal result) =>
        decimal.TryParse(
            value,
            NumberStyles.Number | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture,
            out result);
}
