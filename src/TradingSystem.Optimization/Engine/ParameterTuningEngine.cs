using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Optimization.Models;
using TradingSystem.Optimization.Scoring;

namespace TradingSystem.Optimization.Engine;

public sealed class ParameterTuningEngine(PerformanceScoreCalculator scoreCalculator)
{
    public async Task<IReadOnlyList<ParameterTrial<TOptions>>> RunAsync<TOptions,  TResult>(
        IEnumerable<TOptions> candidates, 
        Func<TOptions, CancellationToken,  Task<TResult>> run, 
        Func<TResult, BotBacktestMetrics> metricsSelector, 
        OptimizationScoreWeights weights, 
        int top  =  50, 
        CancellationToken ct = default)
    {
        var trials  =  new List<ParameterTrial<TOptions>>();
        var sequence  =  0;
        
        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();
            
            var result  =  await run(candidate,  ct);
            var metrics  =  metricsSelector(result);
            
            trials.Add(new ParameterTrial<TOptions>
            {
                Sequence  =  ++sequence, 
                Options  =  candidate, 
                Metrics  =  metrics, 
                Score  =  scoreCalculator.Calculate(metrics,  weights)
            });
        }

        return trials.OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Metrics.NetProfit)
            .Take(Math.Max(1, top))
            .ToArray();
    }
}
