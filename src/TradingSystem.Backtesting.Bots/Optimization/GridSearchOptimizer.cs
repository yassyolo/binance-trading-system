namespace TradingSystem.Backtesting.Bots.Optimization;

public sealed record OptimizationResult<TOptions,  TMetrics>(TOptions Options,  TMetrics Metrics,  decimal Score);

public sealed class GridSearchOptimizer
{
    public async Task<IReadOnlyList<OptimizationResult<TOptions,  TMetrics>>> RunAsync<TOptions,  TResult,  TMetrics>(
        IEnumerable<TOptions> candidates, 
        Func<TOptions,  CancellationToken,  Task<TResult>> run, 
        Func<TResult,  TMetrics> metrics, 
        Func<TMetrics,  decimal> score, 
        int top, 
        CancellationToken cancellationToken  =  default)
    {
        var rows  =  new List<OptimizationResult<TOptions,  TMetrics>>();
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result  =  await run(candidate,  cancellationToken);
            var value  =  metrics(result);
            rows.Add(new(candidate,  value,  score(value)));
        }
        return rows.OrderByDescending(x  =>  x.Score).Take(Math.Max(1,  top)).ToArray();
    }
}
