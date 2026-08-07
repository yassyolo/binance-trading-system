using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using TradingSystem.Strategies.Alligator;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016EntrySignalEvaluator(
    IOptions<Bot8016Options> options, 
    AlligatorEntryPolicy policy)
{
    private readonly Bot8016Options o = options.Value;
    
    public Bot8016EntrySignal? Evaluate(Bot8016Candle c, Bot8016IndicatorSnapshot i)
    {
        var d = policy.Evaluate(new(c.Symbol, c.Interval, c.IsClosed, c.Open, c.High, c.Low, c.Close, i.Teeth, i.Sma200), 
            new(o.Symbol, o.EntryTimeframe, o.EnableLong, o.EnableShort, o.UseMa200Filter, o.MinimumSignalCandleRange));
        
        return d is null 
            ? null : new(d.Side, c, i, d.Reason);
    }
}