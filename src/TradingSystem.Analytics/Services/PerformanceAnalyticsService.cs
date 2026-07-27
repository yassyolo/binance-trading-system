using TradingSystem.Analytics.Abstractions;
using TradingSystem.Analytics.Models;

namespace TradingSystem.Analytics.Services;

public sealed class PerformanceAnalyticsService(IPerformanceAnalyticsStore store)
{
    public async Task SaveCompletedRunAsync(
        PerformanceRun run,
        PerformanceSnapshot snapshot,
        IReadOnlyCollection<PerformanceTrade> trades,
        CancellationToken cancellationToken = default)
    {
        if (run.RunId != snapshot.RunId)
        {
            throw new ArgumentException(
                "The performance run and snapshot must have the same RunId.",
                nameof(snapshot));
        }

        await store.CreateRunAsync(run, cancellationToken);
        await store.SaveTradesAsync(run.RunId, trades, cancellationToken);
        await store.SaveSnapshotAsync(snapshot, cancellationToken);

        // The previous implementation was named SaveCompletedRunAsync but left the
        // persisted run in its initial status. Mark it completed after all details
        // have been stored successfully.
        await store.CompleteRunAsync(
            run.RunId,
            PerformanceRunStatus.Completed,
            run.CompletedAtUtc ?? snapshot.PeriodToUtc,
            run.Notes,
            cancellationToken);
    }
}
