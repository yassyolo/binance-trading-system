using TradingSystem.Analytics.Models;

namespace TradingSystem.Analytics.Abstractions;
public interface IPerformanceAnalyticsStore
{
    Task CreateRunAsync(PerformanceRun run,  CancellationToken cancellationToken  =  default);
    Task CompleteRunAsync(Guid runId,  PerformanceRunStatus status,  DateTime completedAtUtc,  string? notes,  CancellationToken cancellationToken  =  default);
    Task SaveSnapshotAsync(PerformanceSnapshot snapshot,  CancellationToken cancellationToken  =  default);
    Task SaveTradesAsync(Guid runId,  IReadOnlyCollection<PerformanceTrade> trades,  CancellationToken cancellationToken  =  default);
    Task SaveOptimizationTrialsAsync(IReadOnlyCollection<OptimizationTrial> trials,  CancellationToken cancellationToken  =  default);
    Task SaveWalkForwardWindowsAsync(IReadOnlyCollection<WalkForwardWindow> windows,  CancellationToken cancellationToken  =  default);
    Task<IReadOnlyList<PerformanceSnapshot>> QuerySnapshotsAsync(PerformanceQuery query,  CancellationToken cancellationToken  =  default);
    Task<IReadOnlyList<PerformanceRun>> QueryRunsAsync(PerformanceQuery query,  CancellationToken cancellationToken  =  default);
}
