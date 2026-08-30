using TradingSystem.Analytics.Contracts;
using TradingSystem.Analytics.Models;
using TradingSystem.Analytics.Models.Enums;

namespace TradingSystem.Analytics.Services;

public sealed class PerformanceAnalyticsService(IPerformanceAnalyticsStore analyticsStore)
{
    public async Task SaveCompletedRunAsync(PerformanceRun run, PerformanceSnapshot snapshot, IReadOnlyCollection<PerformanceTrade> trades, CancellationToken ct = default)
    {
        if (run.RunId != snapshot.RunId)
            throw new ArgumentException("The performance run and snapshot must have the same RunId.", nameof(snapshot));

        await analyticsStore.CreateRunAsync(run, ct);
        await analyticsStore.SaveTradesAsync(run.RunId, trades, ct);
        await analyticsStore.SaveSnapshotAsync(snapshot, ct);

        await analyticsStore.CompleteRunAsync(run.RunId, PerformanceRunStatus.Completed, run.CompletedAtUtc ?? snapshot.PeriodToUtc, run.Notes, ct);
    }
}
