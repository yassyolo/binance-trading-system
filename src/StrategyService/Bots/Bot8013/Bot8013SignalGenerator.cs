using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8013.Configuration;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Constants;
using TradingSystem.Signals.Models;

namespace StrategyService.Bots.Bot8013;

public sealed class Bot8013SignalGenerator(IOptions<Bot8013Options> options) : ITradingSignalGenerator
{
    private readonly Bot8013Options _options = options.Value;
    
    public string BotName => _options.BotName;
   
    public string StrategyVersion => _options.StrategyVersion;
   
    public IReadOnlyCollection<string> SupportedSymbols => [_options.Symbol];

    public ValueTask<GeneratedTradingSignal?> GenerateAsync(MarketIndicatorSnapshot x, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        if (!TryAlligator(x, out var jaw, out var teeth, out var lips))
            return ValueTask.FromResult<GeneratedTradingSignal?>(null);

        var side = lips > teeth && teeth > jaw  ? "LONG" 
            : lips < teeth && teeth < jaw  ? "SHORT" 
            : null;
        
        if (side is null 
            || (side == "LONG" && !_options.EnableLong) 
            || (side == "SHORT" && !_options.EnableShort))
            return ValueTask.FromResult<GeneratedTradingSignal?>(null);

        return ValueTask.FromResult<GeneratedTradingSignal?>(Create(x, side, "Alligator trend alignment.", jaw, teeth, lips));
    }

    private GeneratedTradingSignal Create(MarketIndicatorSnapshot x, string side, string reason, decimal jaw, decimal teeth, decimal lips) 
        => new(Guid.NewGuid().ToString("N"), 
            BotName, 
            StrategyVersion, 
            x.Symbol, 
            side, 
            "internal-indicators", 
            x.CandleCloseTimeUtc,
            x.CandleOpenTimeUtc, 
            x.Interval, 
            x.Close, 
            reason,
            new Dictionary<string, object?> 
            { 
                ["jaw"] = jaw, 
                ["teeth"] = teeth, 
                ["lips"] = lips, 
                ["close"] = x.Close 
            });

    private static bool TryAlligator(MarketIndicatorSnapshot x, out decimal jaw, out decimal teeth, out decimal lips)
    {
        jaw = 0;
        teeth = 0;
        lips = 0;

        if (!x.TryGet(IndicatorKeys.AlligatorJaw, out jaw))
            return false;

        if (!x.TryGet(IndicatorKeys.AlligatorTeeth, out teeth))
            return false;

        return x.TryGet(IndicatorKeys.AlligatorLips, out lips);
    }
}
