using System.Globalization;using TradingSystem.Contracts.Klines;using TradingSystem.Domain.MarketData;

namespace IndicatorServices;

internal static class KlineMessageMapper
{
    public static bool TryMap(ClosedKlineMessage m, out MarketCandle c)
    {
        c  =  default!;
        if(!D(m.Open, out var o) || !D(m.High, out var h) || !D(m.Low, out var l) || !D(m.Close, out var cl) || !D(m.Volume, out var v))
            return false;
        
        c  =  new(m.Symbol.ToUpperInvariant(), m.Interval.ToLowerInvariant(), DateTimeOffset.FromUnixTimeMilliseconds(m.Time).UtcDateTime, DateTimeOffset.FromUnixTimeMilliseconds(m.CloseTime).UtcDateTime, o, h, l, cl, v, true);
        
        return true;
    }
    
    static bool D(string? x, out decimal d)
         => decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
}
