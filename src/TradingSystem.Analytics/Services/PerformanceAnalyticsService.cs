using TradingSystem.Analytics.Abstractions;
using TradingSystem.Analytics.Models;
namespace TradingSystem.Analytics.Services;
public sealed class PerformanceAnalyticsService(IPerformanceAnalyticsStore store)
{
    public async Task SaveCompletedRunAsync(PerformanceRun run,  PerformanceSnapshot snapshot,  IReadOnlyCollection<PerformanceTrade> trades,  CancellationToken cancellationToken  =  default)
    {
        await store.CreateRunAsync(run,  cancellationToken);
        await store.SaveTradesAsync(run.RunId,  trades,  cancellationToken);
        await store.SaveSnapshotAsync(snapshot,  cancellationToken);
    }
}
