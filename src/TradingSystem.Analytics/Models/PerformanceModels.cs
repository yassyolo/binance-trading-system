namespace TradingSystem.Analytics.Models;

public enum PerformanceRunType { Backtest,  WalkForward,  Optimization,  Live }
public enum PerformanceRunStatus { Running,  Completed,  Failed,  Cancelled }

public sealed record PerformanceMetricSet
{
    public int Signals {  get;  init;  }
    public int OpenedPositions {  get;  init;  }
    public int BlockedSignals {  get;  init;  }
    public int ClosedPositions {  get;  init;  }
    public int WinningPositions {  get;  init;  }
    public int LosingPositions {  get;  init;  }
    public decimal InitialBalance {  get;  init;  }
    public decimal FinalBalance {  get;  init;  }
    public decimal NetProfit {  get;  init;  }
    public decimal ReturnPercent {  get;  init;  }
    public decimal WinRatePercent {  get;  init;  }
    public decimal ProfitFactor {  get;  init;  }
    public decimal MaximumDrawdownAmount {  get;  init;  }
    public decimal MaximumDrawdownPercent {  get;  init;  }
    public decimal TotalFees {  get;  init;  }
    public decimal Expectancy {  get;  init;  }
}

public sealed record PerformanceTrade
{
    public required string PositionId {  get;  init;  }
    public required string Side {  get;  init;  }
    public required DateTime EntryTimeUtc {  get;  init;  }
    public required decimal EntryPrice {  get;  init;  }
    public required DateTime ExitTimeUtc {  get;  init;  }
    public required decimal ExitPrice {  get;  init;  }
    public required decimal Quantity {  get;  init;  }
    public required decimal GrossPnl {  get;  init;  }
    public required decimal Fees {  get;  init;  }
    public required decimal NetPnl {  get;  init;  }
    public required string ExitReason {  get;  init;  }
    
    public bool PartialTakeProfitReached {  get;  init;  }
}

public sealed record PerformanceRun
{
    public required Guid RunId {  get;  init;  }
    
    public required PerformanceRunType RunType {  get;  init;  }
    
    public required string BotName {  get;  init;  }
    
    public required string StrategyVersion {  get;  init;  }
    
    public required string Symbol {  get;  init;  }
    
    public required string Interval {  get;  init;  }
    
    public required DateTime StartedAtUtc {  get;  init;  }
    
    public DateTime? CompletedAtUtc {  get;  init;  }
    
    public PerformanceRunStatus Status {  get;  init;  }
    
    public required string ParametersJson {  get;  init;  }
    
    public string? ParentRunId {  get;  init;  }
    
    public string? Notes {  get;  init;  }
}

public sealed record PerformanceSnapshot
{
    public required Guid RunId {  get;  init;  }
    
    public required string BotName {  get;  init;  }
    
    public required string Symbol {  get;  init;  }
    
    public required DateTime PeriodFromUtc {  get;  init;  }
    
    public required DateTime PeriodToUtc {  get;  init;  }
   
    public required PerformanceMetricSet Metrics {  get;  init;  }
   
    public decimal Score {  get;  init;  }
}

public sealed record OptimizationTrial
{
    public required Guid TrialId {  get;  init;  }
   
    public required Guid OptimizationRunId {  get;  init;  }
   
    public required int Sequence {  get;  init;  }
    
    public required string ParametersJson {  get;  init;  }
   
    public required decimal Score {  get;  init;  }
   
    public required PerformanceMetricSet Metrics {  get;  init;  }
    
    public bool Selected {  get;  init;  }
}

public sealed record WalkForwardWindow
{
    public required Guid WindowId {  get;  init;  }
   
    public required Guid RunId {  get;  init;  }
   
    public required int WindowNumber {  get;  init;  }
    
    public required DateTime TrainFromUtc {  get;  init;  }
    
    public required DateTime TrainToUtc {  get;  init;  }
   
    public required DateTime TestFromUtc {  get;  init;  }
   
    public required DateTime TestToUtc {  get;  init;  }
    
    public required string SelectedParametersJson {  get;  init;  }
    
    public required decimal InSampleScore {  get;  init;  }
   
    public required decimal OutOfSampleScore {  get;  init;  }
   
    public required PerformanceMetricSet InSampleMetrics {  get;  init;  }
    
    public required PerformanceMetricSet OutOfSampleMetrics {  get;  init;  }
}

public sealed record PerformanceQuery
{
    public string? BotName {  get;  init;  }
    
    public string? Symbol {  get;  init;  }
    
    public PerformanceRunType? RunType {  get;  init;  }
   
    public DateTime? FromUtc {  get;  init;  }
   
    public DateTime? ToUtc {  get;  init;  }
   
    public int Take {  get;  init;  }  =  100;
}
