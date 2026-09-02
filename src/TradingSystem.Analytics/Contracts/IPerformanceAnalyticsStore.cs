using TradingSystem.Analytics.Models;
using TradingSystem.Analytics.Models.Enums;

namespace TradingSystem.Analytics.Contracts;

public interface IPerformanceAnalyticsStore
{
    Task CreateRunAsync(PerformanceRun run, CancellationToken ct = default);
    
    Task CompleteRunAsync(Guid runId, PerformanceRunStatus status, DateTime completedAtUtc, string? notes, CancellationToken ct = default);
    
    Task SaveOptimizationTrialsAsync(IReadOnlyCollection<OptimizationTrial> trials, CancellationToken ct = default);
    
    Task SaveWalkForwardWindowsAsync(IReadOnlyCollection<WalkForwardWindow> windows, CancellationToken ct = default);

    Task SaveCompletedBacktestAsync(PerformanceRun run, PerformanceSnapshot snapshot, IReadOnlyCollection<PerformanceTrade> trades, CancellationToken ct = default);   
}
