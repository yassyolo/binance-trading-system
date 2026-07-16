using Microsoft.Extensions.Options;
using StrategyService.Bot8012.Signals.Configuration;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Models;

namespace StrategyService.Bot8012.Signals;

public sealed class Bot8012SignalGenerator(
    IOptions<Bot8012SignalGeneratorOptions> options) : ITradingSignalGenerator
{
    private readonly Bot8012SignalGeneratorOptions _options = options.Value;

    public string BotName => _options.BotName;
    public string StrategyVersion => _options.StrategyVersion;
    public IReadOnlyCollection<string> SupportedSymbols => [_options.Symbol];

    public ValueTask<GeneratedTradingSignal?> GenerateAsync(
        MarketIndicatorSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_options.SignalRules.Enabled)
            return ValueTask.FromResult<GeneratedTradingSignal?>(null);

        var longResult = EvaluateLong(snapshot);
        if (_options.EnableLong && longResult.IsMatch)
            return ValueTask.FromResult<GeneratedTradingSignal?>(Create(snapshot, "Long", longResult.Reason));

        var shortResult = EvaluateShort(snapshot);
        if (_options.EnableShort && shortResult.IsMatch)
            return ValueTask.FromResult<GeneratedTradingSignal?>(Create(snapshot, "Short", shortResult.Reason));

        return ValueTask.FromResult<GeneratedTradingSignal?>(null);
    }

    private RuleResult EvaluateLong(MarketIndicatorSnapshot x)
    {
        var checks = new List<(bool Enabled, bool Available, bool Match, string Name)>();

        checks.Add(Check(x, _options.SignalRules.RequireBollingerBreakout,
            IndicatorKeys.BollingerUpper, upper => x.Close > upper, "close above Bollinger upper"));
        checks.Add(CheckPair(x, _options.SignalRules.RequireSmmaAlignment,
            IndicatorKeys.SmmaFast, IndicatorKeys.SmmaSlow, (fast, slow) => fast > slow, "fast SMMA above slow SMMA"));
        checks.Add(CheckAlligator(x, _options.SignalRules.RequireAlligatorAlignment,
            (lips, teeth, jaw) => lips > teeth && teeth > jaw, "bullish Alligator alignment"));

        return Resolve(checks);
    }

    private RuleResult EvaluateShort(MarketIndicatorSnapshot x)
    {
        var checks = new List<(bool Enabled, bool Available, bool Match, string Name)>();

        checks.Add(Check(x, _options.SignalRules.RequireBollingerBreakout,
            IndicatorKeys.BollingerLower, lower => x.Close < lower, "close below Bollinger lower"));
        checks.Add(CheckPair(x, _options.SignalRules.RequireSmmaAlignment,
            IndicatorKeys.SmmaFast, IndicatorKeys.SmmaSlow, (fast, slow) => fast < slow, "fast SMMA below slow SMMA"));
        checks.Add(CheckAlligator(x, _options.SignalRules.RequireAlligatorAlignment,
            (lips, teeth, jaw) => lips < teeth && teeth < jaw, "bearish Alligator alignment"));

        return Resolve(checks);
    }

    private GeneratedTradingSignal Create(MarketIndicatorSnapshot x, string side, string reason)
        => new(
            Guid.NewGuid().ToString("N"), BotName, StrategyVersion, x.Symbol, side,
            "Internal", x.CandleCloseTimeUtc, x.CandleOpenTimeUtc, x.Interval,
            x.Close, reason,
            new Dictionary<string, object?>
            {
                ["open"] = x.Open,
                ["high"] = x.High,
                ["low"] = x.Low,
                ["close"] = x.Close,
                ["volume"] = x.Volume,
                ["indicators"] = x.Indicators
            });

    private static RuleResult Resolve(IEnumerable<(bool Enabled, bool Available, bool Match, string Name)> checks)
    {
        var active = checks.Where(x => x.Enabled).ToArray();
        if (active.Length == 0) return new(false, "No enabled signal rules.");
        if (active.Any(x => !x.Available))
            return new(false, "Missing required indicators: " + string.Join(", ", active.Where(x => !x.Available).Select(x => x.Name)));
        if (active.Any(x => !x.Match)) return new(false, "Signal rules did not align.");
        return new(true, string.Join("; ", active.Select(x => x.Name)));
    }

    private static (bool, bool, bool, string) Check(
        MarketIndicatorSnapshot x, bool enabled, string key, Func<decimal, bool> predicate, string name)
        => !enabled ? (false, true, true, name)
            : x.TryGet(key, out var value) ? (true, true, predicate(value), name)
            : (true, false, false, name);

    private static (bool, bool, bool, string) CheckPair(
        MarketIndicatorSnapshot x, bool enabled, string firstKey, string secondKey,
        Func<decimal, decimal, bool> predicate, string name)
        => !enabled ? (false, true, true, name)
            : x.TryGet(firstKey, out var first) && x.TryGet(secondKey, out var second)
                ? (true, true, predicate(first, second), name)
                : (true, false, false, name);

    private static (bool, bool, bool, string) CheckAlligator(
        MarketIndicatorSnapshot x, bool enabled,
        Func<decimal, decimal, decimal, bool> predicate, string name)
        => !enabled ? (false, true, true, name)
            : x.TryGet(IndicatorKeys.AlligatorLips, out var lips) &&
              x.TryGet(IndicatorKeys.AlligatorTeeth, out var teeth) &&
              x.TryGet(IndicatorKeys.AlligatorJaw, out var jaw)
                ? (true, true, predicate(lips, teeth, jaw), name)
                : (true, false, false, name);

    private sealed record RuleResult(bool IsMatch, string Reason);
}
