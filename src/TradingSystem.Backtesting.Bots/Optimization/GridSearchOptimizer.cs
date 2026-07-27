using System.Collections.Concurrent;

namespace TradingSystem.Backtesting.Bots.Optimization;

public sealed record OptimizationResult<TOptions, TMetrics>(
    TOptions Options,
    TMetrics Metrics,
    decimal Score);

public sealed class GridSearchOptimizer
{
    public async Task<IReadOnlyList<OptimizationResult<TOptions, TMetrics>>> RunAsync<TOptions, TResult, TMetrics>(
        IEnumerable<TOptions> candidates,
        Func<TOptions, CancellationToken, Task<TResult>> run,
        Func<TResult, TMetrics> metrics,
        Func<TMetrics, decimal> score,
        int top,
        CancellationToken cancellationToken = default,
        int? maximumDegreeOfParallelism = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(score);

        var candidateSet = candidates.ToArray();
        if (candidateSet.Length == 0)
            return [];

        var rows = new ConcurrentBag<(int Sequence, OptimizationResult<TOptions, TMetrics> Result)>();
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Max(
                1,
                maximumDegreeOfParallelism ?? Math.Min(Environment.ProcessorCount, 8))
        };

        await Parallel.ForEachAsync(
            candidateSet.Select((candidate, sequence) => (candidate, sequence)),
            parallelOptions,
            async (item, ct) =>
            {
                var result = await run(item.candidate, ct);
                var metricValue = metrics(result);
                rows.Add((
                    item.sequence,
                    new OptimizationResult<TOptions, TMetrics>(
                        item.candidate,
                        metricValue,
                        score(metricValue))));
            });

        return rows
            .OrderByDescending(x => x.Result.Score)
            .ThenBy(x => x.Sequence)
            .Take(Math.Max(1, top))
            .Select(x => x.Result)
            .ToArray();
    }
}
