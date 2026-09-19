using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using StrategyService.Bots.Bot8016.Models;
using TradingSystem.Strategies.Alligator;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016EntrySignalEvaluator(
    IOptions<Bot8016Options> options, 
    AlligatorEntryPolicy alligatorPolicy)
{
    private readonly Bot8016Options _options = options.Value;
    
    public Bot8016EntrySignal? Evaluate(Bot8016Candle c, Bot8016IndicatorSnapshot i)
    {
        var decision = alligatorPolicy.Evaluate(new(c.Symbol, c.Interval, c.IsClosed, c.Open, c.High, c.Low, c.Close, i.Teeth, i.Sma200), 
            new(_options.Symbol, _options.EntryTimeframe, _options.EnableLong, _options.EnableShort, _options.UseMa200Filter, _options.MinimumSignalCandleRange));
        
        return decision is null  ? null : new(decision.Side, c, i, decision.Reason);
    }
}