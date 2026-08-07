using TradingSystem.Analytics.Models;
using TradingSystem.Analytics.Models.Enums;

namespace TradingSystem.Analytics.Contracts;

public interface IPerformanceAnalyticsStore
{
    Task CreateRunAsync(PerformanceRun run, CancellationToken cancellationToken = default);
    
    Task CompleteRunAsync(Guid runId, PerformanceRunStatus status, DateTime completedAtUtc, string? notes, CancellationToken cancellationToken = default);
    
    Task SaveSnapshotAsync(PerformanceSnapshot snapshot, CancellationToken cancellationToken = default);
    
    Task SaveTradesAsync(Guid runId, IReadOnlyCollection<PerformanceTrade> trades, CancellationToken cancellationToken = default);
    
    Task SaveOptimizationTrialsAsync(IReadOnlyCollection<OptimizationTrial> trials, CancellationToken cancellationToken = default);
    
    Task SaveWalkForwardWindowsAsync(IReadOnlyCollection<WalkForwardWindow> windows, CancellationToken cancellationToken = default);

    Task SaveCompletedBacktestAsync(PerformanceRun run, PerformanceSnapshot snapshot, IReadOnlyCollection<PerformanceTrade> trades, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PerformanceSnapshot>> QuerySnapshotsAsync(PerformanceQuery query, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<PerformanceRun>> QueryRunsAsync(PerformanceQuery query, CancellationToken cancellationToken = default);
}
