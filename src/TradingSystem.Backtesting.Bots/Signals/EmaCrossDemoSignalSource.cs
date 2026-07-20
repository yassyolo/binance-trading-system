using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Models;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Signals;

public sealed class EmaCrossDemoSignalSource(int fastPeriod  =  20,  int slowPeriod  =  50)
{
    public IReadOnlyList<HistoricalBotSignal> Generate(IReadOnlyList<MarketCandle> candles)
    {
        var result  =  new List<HistoricalBotSignal>();
        decimal? fast  =  null,  slow  =  null,  previousFast  =  null,  previousSlow  =  null;
        var fastAlpha  =  2m / (fastPeriod + 1m);
        var slowAlpha  =  2m / (slowPeriod + 1m);
        foreach (var candle in candles.OrderBy(x  =>  x.OpenTimeUtc))
        {
            fast  =  fast is null ? candle.Close : fast + fastAlpha * (candle.Close - fast.Value);
            slow  =  slow is null ? candle.Close : slow + slowAlpha * (candle.Close - slow.Value);
            if (previousFast.HasValue  &&  previousSlow.HasValue)
            {
                if (previousFast <= previousSlow  &&  fast > slow) result.Add(new(candle.CloseTimeUtc,  TradeSide.Long,  "EMA_DEMO"));
                else if (previousFast >= previousSlow  &&  fast < slow) result.Add(new(candle.CloseTimeUtc,  TradeSide.Short,  "EMA_DEMO"));
            }
            previousFast  =  fast; previousSlow  =  slow;
        }
        return result;
    }
}
