using TradingSystem.Backtesting.Bot8012.Engine;
using TradingSystem.Backtesting.Bot8012.Models;
namespace TradingSystem.Backtesting.Bot8012.Optimization;
public sealed record OptimizationRow(Bot8012BacktestOptions Options, BacktestMetrics Metrics, decimal Score);
public sealed class Bot8012Optimizer
{
    public IReadOnlyList<OptimizationRow> Run(IReadOnlyList<Candle> candles, IReadOnlyList<HistoricalSignal> signals, Bot8012BacktestOptions seed) { var rows = new List<OptimizationRow>(); foreach (var profit in new[] { 100m, 150m, 200m, 250m, 300m }) foreach (var gap in new[] { 200m, 300m, 400m, 500m, 600m }) foreach (var limit in new[] { 1, 2, 3 }) foreach (var cooldown in new[] { 0, 60, 180, 300 }) { var o = seed with { ProfitDistance = profit, PriceDistance = gap, OrderSideLimit = limit, CooldownSeconds = cooldown }; var r = new Bot8012BacktestEngine().Run(candles, signals, o); var score = r.Metrics.NetProfit - r.Metrics.MaxDrawdownPercent * 2m + (r.Metrics.ProfitFactor == decimal.MaxValue ? 5 : r.Metrics.ProfitFactor); rows.Add(new(o, r.Metrics, score)); } return rows.OrderByDescending(x => x.Score).ToArray(); }
}
