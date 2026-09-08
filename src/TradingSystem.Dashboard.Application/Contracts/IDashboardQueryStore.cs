using TradingSystem.Dashboard.Application.Models;
using TradingSystem.Dashboard.Contracts.Models.Alerts;
using TradingSystem.Dashboard.Contracts.Models.Analytics;
using TradingSystem.Dashboard.Contracts.Models.Audit;
using TradingSystem.Dashboard.Contracts.Models.Charts;
using TradingSystem.Dashboard.Contracts.Models.Health;
using TradingSystem.Dashboard.Contracts.Models.Optimization;
using TradingSystem.Dashboard.Contracts.Models.Positions;
using TradingSystem.Dashboard.Contracts.Models.Signals;
using TradingSystem.Dashboard.Contracts.Models.Trades;

namespace TradingSystem.Dashboard.Application.Contracts;

public interface IDashboardQueryStore
{
    Task<LiveOverviewDto> GetOverviewAsync(CancellationToken ct);
    
    Task<IReadOnlyCollection<SignalRowDto>> GetSignalsAsync(DashboardQuery query, CancellationToken ct);
   
    Task<IReadOnlyCollection<PositionRowDto>> GetPositionsAsync(DashboardQuery query, CancellationToken ct);
    
    Task<IReadOnlyCollection<TradeHistoryRowDto>> GetTradesAsync(DashboardQuery query, CancellationToken ct);
    
    Task<AnalyticsSummaryDto> GetAnalyticsAsync(DashboardQuery query, CancellationToken ct);
    
    Task<IReadOnlyCollection<EquityPointDto>> GetEquityAsync(DashboardQuery query, CancellationToken ct);
    
    Task<PriceChartDto> GetPriceChartAsync(string symbol, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    
    Task<IReadOnlyCollection<RunSummaryDto>> GetRunsAsync(DashboardQuery query, CancellationToken ct);
    
    Task<IReadOnlyCollection<OptimizationTrialDto>> GetOptimizationTrialsAsync(Guid runId, int take, CancellationToken ct);
    
    Task<StrategyComparisonDto?> CompareRunsAsync(Guid leftRunId, Guid rightRunId, CancellationToken ct);
    
    Task<IReadOnlyCollection<ComponentHealthDto>> GetHealthAsync(CancellationToken ct);
    
    Task<IReadOnlyCollection<AlertDto>> GetAlertsAsync(bool acknowledged, int take, CancellationToken ct);
   
    Task<IReadOnlyCollection<AuditEventDto>> GetAuditEventsAsync(
        string? actor, 
        string? action, 
        DateTime? fromUtc,
        DateTime? toUtc, 
        int skip,
        int take, 
        CancellationToken ct);
}
