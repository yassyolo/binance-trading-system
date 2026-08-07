using TradingSystem.Analytics.Contracts;
using TradingSystem.Analytics.Models;
using TradingSystem.Analytics.Models.Enums;

namespace TradingSystem.Analytics.Services;

public sealed class PerformanceAnalyticsService(IPerformanceAnalyticsStore store)
{
    public async Task SaveCompletedRunAsync(PerformanceRun run, PerformanceSnapshot snapshot, IReadOnlyCollection<PerformanceTrade> trades, CancellationToken ct = default)
    {
        if (run.RunId != snapshot.RunId)
            throw new ArgumentException("The performance run and snapshot must have the same RunId.", nameof(snapshot));

        await store.CreateRunAsync(run, ct);
        await store.SaveTradesAsync(run.RunId, trades, ct);
        await store.SaveSnapshotAsync(snapshot, ct);

        await store.CompleteRunAsync(run.RunId, PerformanceRunStatus.Completed, run.CompletedAtUtc ?? snapshot.PeriodToUtc, run.Notes, ct);
    }
}
