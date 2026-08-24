using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Constants;
using TradingSystem.Signals.Models;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012SignalGenerator(
    IOptions<Bot8012Options> options)
    :ITradingSignalGenerator
{
    private readonly Bot8012Options _o = options.Value;
    
    public string BotName => _o.BotName;
    
    public string StrategyVersion => _o.StrategyVersion;
   
    public IReadOnlyCollection<string> SupportedSymbols => [_o.Symbol];
 
    public ValueTask<GeneratedTradingSignal?> GenerateAsync(MarketIndicatorSnapshot x, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        if(!_o.SignalRules.Enabled)
            return ValueTask.FromResult<GeneratedTradingSignal?>(null);
        
        var side = Match(x, true) 
            ? "LONG" : Match(x, false)
            ? "SHORT" : null;
        
        if(side is null 
            || (side=="LONG" && !_o.EnableLong) 
            || (side=="SHORT" && !_o.EnableShort))
            return ValueTask.FromResult<GeneratedTradingSignal?>(null);
        
        return ValueTask.FromResult<GeneratedTradingSignal?>(new(
            Guid.NewGuid().ToString("N"), 
            BotName, 
            StrategyVersion, 
            x.Symbol, 
            side, 
            "internal-indicators", 
            x.CandleCloseTimeUtc, 
            x.CandleOpenTimeUtc, 
            x.Interval, 
            x.Close, 
            "Configured indicators aligned.", 
            new Dictionary<string, object?>{{"indicators", x.Indicators}}));   
    }
    
    private bool Match(MarketIndicatorSnapshot x, bool longSide)
    {
        var r = _o.SignalRules;
        
        if(r.RequireBollingerBreakout 
            && (!x.TryGet(longSide ? IndicatorKeys.BollingerUpper : IndicatorKeys.BollingerLower, out var b) 
            || (longSide ? x.Close <= b : x.Close >= b)))
            return false;
        
        if(r.RequireSmmaAlignment 
            && (!x.TryGet(IndicatorKeys.SmmaFast, out var f) 
            || !x.TryGet(IndicatorKeys.SmmaSlow, out var s) 
            || (longSide ? f <= s : f >= s)))
            return false;
        
        if(r.RequireAlligatorAlignment 
            && (!x.TryGet(IndicatorKeys.AlligatorLips, out var l) 
            || !x.TryGet(IndicatorKeys.AlligatorTeeth, out var t) 
            || !x.TryGet(IndicatorKeys.AlligatorJaw, out var j) 
            || (longSide ? !(l > t && t > j) : !(l < t && t < j))))
            return false;
        
        return true;
    }
}
