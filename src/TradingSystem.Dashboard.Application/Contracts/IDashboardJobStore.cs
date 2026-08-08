using TradingSystem.Dashboard.Contracts.Models.Backtesting;
using TradingSystem.Dashboard.Contracts.Models.Jobs;
using TradingSystem.Dashboard.Contracts.Models.Optimization;

namespace TradingSystem.Dashboard.Application.Contracts;

public interface IDashboardJobStore
{
    Task<JobAcceptedDto> EnqueueBacktestAsync(BacktestRequest request, string user, CancellationToken ct);
    
    Task<JobAcceptedDto> EnqueueOptimizationAsync(OptimizationRequest request, string user, CancellationToken ct);
}
