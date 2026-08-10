using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8014.Configuration;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Constants;
using TradingSystem.Signals.Models;

namespace StrategyService.Bots.Bot8014;

public sealed class Bot8014SignalGenerator(IOptions<Bot8014Options> options) : ITradingSignalGenerator
{
    private readonly Bot8014Options _options = options.Value;
    public string BotName => _options.BotName;
    public string StrategyVersion => _options.StrategyVersion;
    public IReadOnlyCollection<string> SupportedSymbols => [_options.Symbol];

    public ValueTask<GeneratedTradingSignal?> GenerateAsync(MarketIndicatorSnapshot x, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!x.TryGet(IndicatorKeys.BollingerUpper, out var upper) || !x.TryGet(IndicatorKeys.BollingerLower, out var lower))
            return ValueTask.FromResult<GeneratedTradingSignal?>(null);

        var side = x.Close > upper ? "LONG" : x.Close < lower ? "SHORT" : null;
        if (side is null || (side == "LONG" && !_options.EnableLong) || (side == "SHORT" && !_options.EnableShort))
            return ValueTask.FromResult<GeneratedTradingSignal?>(null);

        return ValueTask.FromResult<GeneratedTradingSignal?>(new(
            Guid.NewGuid().ToString("N"), BotName, StrategyVersion, x.Symbol, side, "internal-indicators", x.CandleCloseTimeUtc,
            x.CandleOpenTimeUtc, x.Interval, x.Close, "BB20 close breakout.",
            new Dictionary<string, object?> { ["upper"] = upper, ["lower"] = lower, ["close"] = x.Close }));
    }
}
