using TradingSystem.Dashboard.Contracts;
namespace TradingSystem.Dashboard.Application;

public sealed record DashboardQuery(int Skip  =  0,  int Take  =  100,  string? BotName  =  null,  string? Symbol  =  null,  DateTime? FromUtc  =  null,  DateTime? ToUtc  =  null,  string? Status  =  null);
public interface IDashboardQueryStore
{
 Task<LiveOverviewDto> GetOverviewAsync(CancellationToken ct);
 Task<IReadOnlyCollection<SignalRowDto>> GetSignalsAsync(DashboardQuery query,  CancellationToken ct);
 Task<IReadOnlyCollection<PositionRowDto>> GetPositionsAsync(DashboardQuery query,  CancellationToken ct);
 Task<IReadOnlyCollection<TradeHistoryRowDto>> GetTradesAsync(DashboardQuery query,  CancellationToken ct);
 Task<AnalyticsSummaryDto> GetAnalyticsAsync(DashboardQuery query,  CancellationToken ct);
 Task<IReadOnlyCollection<EquityPointDto>> GetEquityAsync(DashboardQuery query,  CancellationToken ct);
 Task<PriceChartDto> GetPriceChartAsync(string symbol,  string interval,  DateTime fromUtc,  DateTime toUtc,  CancellationToken ct);
 Task<IReadOnlyCollection<RunSummaryDto>> GetRunsAsync(DashboardQuery query,  CancellationToken ct);
 Task<IReadOnlyCollection<OptimizationTrialDto>> GetOptimizationTrialsAsync(Guid runId,  int take,  CancellationToken ct);
 Task<StrategyComparisonDto?> CompareRunsAsync(Guid leftRunId,  Guid rightRunId,  CancellationToken ct);
 Task<IReadOnlyCollection<ComponentHealthDto>> GetHealthAsync(CancellationToken ct);
 Task<IReadOnlyCollection<AlertDto>> GetAlertsAsync(bool acknowledged,  int take,  CancellationToken ct);
 Task<IReadOnlyCollection<AuditEventDto>> GetAuditEventsAsync(string? actor, string? action, DateTime? fromUtc, DateTime? toUtc, int skip, int take, CancellationToken ct);
}
public interface IBotConfigurationStore
{
 Task<IReadOnlyCollection<BotConfigurationDto>> GetAllAsync(CancellationToken ct);
 Task<BotConfigurationDto?> GetAsync(string botName,  CancellationToken ct);
 Task<BotConfigurationDto> UpdateAsync(string botName,  UpdateBotConfigurationRequest request,  string user,  CancellationToken ct);
}
public interface IBotCommandStore
{
 Task<BotCommandDto> EnqueueAsync(string botName,  BotCommandRequest request,  string user,  CancellationToken ct);
 Task<IReadOnlyCollection<BotCommandDto>> GetAsync(string? botName,  int take,  CancellationToken ct);
}
public interface IDashboardJobStore
{
 Task<JobAcceptedDto> EnqueueBacktestAsync(BacktestRequest request,  string user,  CancellationToken ct);
 Task<JobAcceptedDto> EnqueueOptimizationAsync(OptimizationRequest request,  string user,  CancellationToken ct);
}
public interface IAlertCommandStore { Task AcknowledgeAsync(long alertId,  string user,  CancellationToken ct); }
