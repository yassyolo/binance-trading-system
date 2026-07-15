using System.Globalization;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Strategies;

public sealed class EmaCrossStrategy(int fastPeriod, int slowPeriod, decimal stopLossPercent, decimal takeProfitPercent)
    : IBacktestStrategy
{
    public string Name => "EMA_CROSS";
    public int WarmupBars => slowPeriod + 2;

    public ValueTask<StrategyDecision> DecideAsync(BacktestStrategyContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.History.Count < WarmupBars)
            return ValueTask.FromResult(StrategyDecision.None("Warmup"));

        var closes = context.History.Select(x => x.Close).ToArray();
        var previousFast = CalculateEma(closes.AsSpan(0, closes.Length - 1), fastPeriod);
        var previousSlow = CalculateEma(closes.AsSpan(0, closes.Length - 1), slowPeriod);
        var currentFast = CalculateEma(closes, fastPeriod);
        var currentSlow = CalculateEma(closes, slowPeriod);
        var price = context.CurrentCandle.Close;

        if (previousFast <= previousSlow && currentFast > currentSlow)
        {
            var sl = price * (1m - stopLossPercent / 100m);
            var tp = price * (1m + takeProfitPercent / 100m);
            return ValueTask.FromResult(context.ActivePosition?.Side == TradeSide.Short
                ? StrategyDecision.Reverse(TradeSide.Long, sl, tp, "Bullish EMA crossover")
                : context.ActivePosition is null
                    ? StrategyDecision.Open(TradeSide.Long, sl, tp, "Bullish EMA crossover")
                    : StrategyDecision.None("Long already open"));
        }

        if (previousFast >= previousSlow && currentFast < currentSlow)
        {
            var sl = price * (1m + stopLossPercent / 100m);
            var tp = price * (1m - takeProfitPercent / 100m);
            return ValueTask.FromResult(context.ActivePosition?.Side == TradeSide.Long
                ? StrategyDecision.Reverse(TradeSide.Short, sl, tp, "Bearish EMA crossover")
                : context.ActivePosition is null
                    ? StrategyDecision.Open(TradeSide.Short, sl, tp, "Bearish EMA crossover")
                    : StrategyDecision.None("Short already open"));
        }

        return ValueTask.FromResult(StrategyDecision.None());
    }

    private static decimal CalculateEma(ReadOnlySpan<decimal> values, int period)
    {
        if (values.Length == 0) return 0m;
        var multiplier = 2m / (period + 1m);
        var ema = values[0];
        for (var i = 1; i < values.Length; i++)
            ema = (values[i] - ema) * multiplier + ema;
        return ema;
    }
}

public sealed class EmaCrossStrategyFactory : IBacktestStrategyFactory
{
    public string Name => "EMA_CROSS";

    public IBacktestStrategy Create(IReadOnlyDictionary<string, string> parameters)
        => new EmaCrossStrategy(
            GetInt(parameters, "fast", 20),
            GetInt(parameters, "slow", 50),
            GetDecimal(parameters, "slPercent", 1m),
            GetDecimal(parameters, "tpPercent", 2m));

    private static int GetInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
        => values.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result : fallback;

    private static decimal GetDecimal(IReadOnlyDictionary<string, string> values, string key, decimal fallback)
        => values.TryGetValue(key, out var value) && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result : fallback;
}
