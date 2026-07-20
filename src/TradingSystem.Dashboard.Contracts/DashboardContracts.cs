namespace TradingSystem.Dashboard.Contracts;

public enum BotRuntimeStatus { Stopped,  Running,  Paused,  EmergencyStopped,  Faulted }
public enum TradingEnvironment { Demo,  Production }
public enum BotCommandType { Start,  Stop,  Pause,  Resume,  EmergencyStop,  ClosePosition,  CancelTakeProfit,  RecreateTakeProfit }
public enum BotCommandStatus { Pending,  Processing,  Completed,  Failed,  Rejected }

public sealed record BotOverviewDto(
    string BotName,  BotRuntimeStatus Status,  TradingEnvironment Environment, 
    string SignalSource,  string? LastSignalSide,  DateTime? LastSignalAtUtc, 
    string? LastDecision,  string? LastDecisionReason,  DateTime? LastDecisionAtUtc, 
    int OpenPositions,  decimal UnrealizedPnl,  decimal RealizedPnlToday, 
    string StrategyVersion,  DateTime? LastHeartbeatUtc);

public sealed record LiveOverviewDto(
    DateTime GeneratedAtUtc,  IReadOnlyCollection<BotOverviewDto> Bots, 
    IReadOnlyCollection<ComponentHealthDto> Components, 
    decimal RealizedPnlToday,  decimal UnrealizedPnl,  int OpenPositions, 
    int CriticalAlerts);

public sealed record ComponentHealthDto(string Component,  string Status,  DateTime? LastSeenUtc,  string? Details);
public sealed record SignalRowDto(long Id,  string SignalId,  DateTime TimeUtc,  string BotName,  string Symbol,  string Source,  string Side,  decimal? Price,  string? Decision,  string? BlockReason,  string StrategyVersion,  string Environment);
public sealed record PositionRowDto(string PositionId,  string BotName,  string Symbol,  string Side,  string Status,  decimal Quantity,  decimal? EntryPrice,  decimal? TakeProfitPrice,  decimal? CurrentPrice,  decimal UnrealizedPnl,  decimal? RealizedPnl,  DateTime OpenedAtUtc,  DateTime? ClosedAtUtc,  string StrategyVersion,  string Environment);
public sealed record TradeHistoryRowDto(string PositionId,  string BotName,  string Symbol,  string Side,  decimal? EntryPrice,  decimal? ExitPrice,  decimal Quantity,  decimal? RealizedPnl,  decimal? Fees,  TimeSpan? Duration,  string? Source,  string StrategyVersion,  string Environment,  string? CloseReason,  DateTime OpenedAtUtc,  DateTime? ClosedAtUtc);
public sealed record AnalyticsSummaryDto(decimal TotalPnl,  decimal DailyPnl,  decimal WeeklyPnl,  decimal MonthlyPnl,  decimal WinRate,  decimal AverageWin,  decimal AverageLoss,  decimal MaxDrawdownPercent,  decimal AverageHoldingMinutes,  int Signals,  int OpenedSignals,  int BlockedSignals,  IReadOnlyDictionary<string, int> BlockReasons,  decimal LongPnl,  decimal ShortPnl);
public sealed record EquityPointDto(DateTime TimeUtc,  decimal Equity,  decimal DrawdownPercent);
public sealed record PriceCandleDto(DateTime OpenTimeUtc,  decimal Open,  decimal High,  decimal Low,  decimal Close,  decimal Volume);
public sealed record ChartMarkerDto(DateTime TimeUtc,  string Kind,  string Side,  decimal Price,  string? Label);
public sealed record PriceChartDto(IReadOnlyCollection<PriceCandleDto> Candles,  IReadOnlyCollection<ChartMarkerDto> Markers);

public sealed record BotConfigurationDto(string BotName,  string StrategyType,  string Symbol,  TradingEnvironment Environment,  string SignalSource,  bool EnableLong,  bool EnableShort,  decimal Quantity,  int Leverage,  decimal? PriceDistance,  decimal? ProfitDistance,  int? OrderSideLimit,  int CooldownSeconds,  long Version,  DateTime UpdatedAtUtc,  string UpdatedBy,  bool RestartRequired);
public sealed record UpdateBotConfigurationRequest(long ExpectedVersion,  string StrategyType,  string Symbol,  TradingEnvironment Environment,  string SignalSource,  bool EnableLong,  bool EnableShort,  decimal Quantity,  int Leverage,  decimal? PriceDistance,  decimal? ProfitDistance,  int? OrderSideLimit,  int CooldownSeconds,  string Reason);
public sealed record BotCommandRequest(BotCommandType Command,  string Reason,  bool Confirmed,  bool CancelOpenOrders  =  false,  bool CloseOpenPositions  =  false,  string? PositionId  =  null);
public sealed record BotCommandDto(Guid CommandId,  string BotName,  BotCommandType Command,  BotCommandStatus Status,  string RequestedBy,  string Reason,  DateTime RequestedAtUtc,  DateTime? CompletedAtUtc,  string? Error);

public sealed record BacktestRequest(string BotName,  string Symbol,  DateTime FromUtc,  DateTime ToUtc,  decimal InitialBalance,  string SignalSource,  decimal CommissionPercent,  decimal SlippagePercent,  IReadOnlyDictionary<string, string> Parameters);
public sealed record OptimizationRangeDto(string Name,  decimal From,  decimal To,  decimal Step);
public sealed record OptimizationRequest(string BotName,  string Symbol,  DateTime FromUtc,  DateTime ToUtc,  decimal InitialBalance,  string SignalSource,  IReadOnlyCollection<OptimizationRangeDto> Ranges,  int TopResults,  bool WalkForward,  int? TrainBars,  int? TestBars,  int? StepBars);
public sealed record JobAcceptedDto(Guid JobId,  string Type,  string Status,  DateTime CreatedAtUtc);
public sealed record RunSummaryDto(Guid RunId,  string RunType,  string BotName,  string StrategyVersion,  string Symbol,  string Interval,  DateTime StartedAtUtc,  DateTime? CompletedAtUtc,  string Status,  decimal? NetProfit,  decimal? WinRatePercent,  decimal? MaxDrawdownPercent,  decimal? Score);
public sealed record OptimizationTrialDto(Guid TrialId,  int Rank,  IReadOnlyDictionary<string, string> Parameters,  decimal Score,  decimal NetProfit,  decimal DrawdownPercent,  decimal WinRatePercent,  int Trades,  bool Selected);
public sealed record StrategyComparisonDto(Guid LeftRunId,  Guid RightRunId,  string LeftLabel,  string RightLabel,  decimal NetProfitDifference,  decimal DrawdownDifference,  decimal WinRateDifference,  decimal ScoreDifference);
public sealed record AlertDto(long AlertId,  string Severity,  string Type,  string Message,  string? BotName,  string? PositionId,  DateTime CreatedAtUtc,  bool Acknowledged,  DateTime? AcknowledgedAtUtc,  string? AcknowledgedBy);

public sealed record AuditEventDto(Guid AuditId, DateTime OccurredAtUtc, string Actor, string Action, string EntityType, string? EntityId, string? Reason, string? CorrelationId, string? IpAddress, string? OldValueJson, string? NewValueJson);
