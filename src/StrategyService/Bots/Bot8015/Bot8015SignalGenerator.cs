using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8015.Configuration;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Constants;
using TradingSystem.Signals.Models;

namespace StrategyService.Bots.Bot8015;

public sealed class Bot8015SignalGenerator(
    IOptions<Bot8015Options> options) 
    : ITradingSignalGenerator
{
    private const string Sma200 = "sma200";
    private readonly Bot8015Options _options = options.Value;
   
    public string BotName => _options.BotName;
   
    public string StrategyVersion => _options.StrategyVersion;
    
    public IReadOnlyCollection<string> SupportedSymbols => [_options.Symbol];

    public ValueTask<GeneratedTradingSignal?> GenerateAsync(MarketIndicatorSnapshot x, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!x.TryGet(IndicatorKeys.AlligatorJaw, out var jaw) ||
            !x.TryGet(IndicatorKeys.AlligatorTeeth, out var teeth) ||
            !x.TryGet(IndicatorKeys.AlligatorLips, out var lips) ||
            !x.TryGet(Sma200, out var sma200))
            return ValueTask.FromResult<GeneratedTradingSignal?>(null);

        var longMatch = lips > teeth && teeth > jaw && x.Close > sma200;
        var shortMatch = lips < teeth && teeth < jaw && x.Close < sma200;
       
        var side = longMatch ? "LONG" 
            : shortMatch ? "SHORT"
            : null;

        if (side is null 
            || (side == "LONG" && !_options.EnableLong) 
            || (side == "SHORT" && !_options.EnableShort))
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
            "Alligator alignment confirmed by SMA200 trend filter.",
            new Dictionary<string, object?> { ["jaw"] = jaw, ["teeth"] = teeth, ["lips"] = lips, ["sma200"] = sma200, ["close"] = x.Close }
            ));
    }
}
