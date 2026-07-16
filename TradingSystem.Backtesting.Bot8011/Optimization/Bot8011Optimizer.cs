using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Bot8011.Engine;
using TradingSystem.Backtesting.Bot8011.Models;

namespace TradingSystem.Backtesting.Bot8011.Optimization;

public sealed record Bot8011OptimizationSpace
{
    public IReadOnlyList<decimal> TakeProfitPercents { get; init; } = [0.08m, 0.10m, 0.12m, 0.15m, 0.20m];
    public IReadOnlyList<decimal> StopLossPercents { get; init; } = [0.10m, 0.15m, 0.20m, 0.25m, 0.30m];
    public IReadOnlyList<decimal> Stop3EntryOffsets { get; init; } = [0m, 25m, 50m, 100m];
    public IReadOnlyList<decimal> Stop3TrailingSteps { get; init; } = [100m, 200m, 300m, 400m, 500m];
    public IReadOnlyList<decimal> Stop3TrailingBuffers { get; init; } = [25m, 50m, 75m, 100m];
    public IReadOnlyList<int> CooldownSeconds { get; init; } = [0, 60, 120, 180, 300];
}

public sealed record Bot8011OptimizationRow(Bot8011BacktestOptions Options, Bot8011Metrics Metrics, decimal Score);

public sealed class Bot8011Optimizer(Bot8011BacktestEngine engine)
{
    public async Task<IReadOnlyList<Bot8011OptimizationRow>> RunAsync(decimal initialBalance,
        Bot8011BacktestOptions baseline, Bot8011OptimizationSpace space,
        IReadOnlyList<HistoricalCandle> candles, IReadOnlyList<Bot8011Signal> signals,
        int top = 50, CancellationToken cancellationToken = default)
    {
        var rows = new List<Bot8011OptimizationRow>();
        foreach (var tp in space.TakeProfitPercents)
            foreach (var sl in space.StopLossPercents)
                foreach (var offset in space.Stop3EntryOffsets)
                    foreach (var step in space.Stop3TrailingSteps)
                        foreach (var buffer in space.Stop3TrailingBuffers)
                            foreach (var cooldown in space.CooldownSeconds)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                var options = baseline with
                                {
                                    TakeProfitPercent = tp,
                                    StopLossPercent = sl,
                                    Stop3EntryOffset = offset,
                                    Stop3TrailingStep = step,
                                    Stop3TrailingBuffer = buffer,
                                    CooldownSeconds = cooldown
                                };
                                var result = await engine.RunAsync(initialBalance, options, candles, signals, cancellationToken);
                                var m = result.Metrics;
                                var activityPenalty = m.Positions < 10 ? (10 - m.Positions) * 2m : 0m;
                                var score = m.ReturnPercent + Math.Min(m.ProfitFactor, 5m) * 5m - m.MaximumDrawdownPercent * 1.5m - activityPenalty;
                                rows.Add(new(options, m, score));
                            }
        return rows.OrderByDescending(x => x.Score).ThenByDescending(x => x.Metrics.NetProfit).Take(top).ToArray();
    }
}
