namespace TradingSystem.Backtesting.Models;

public enum TradeSide { Long,  Short }
public enum EntryExecutionMode { SignalCandleClose,  NextCandleOpen }
public enum IntrabarConflictPolicy { StopLossFirst,  TakeProfitFirst,  WorstCase,  BestCase }
public enum ExitReason { TakeProfit,  StopLoss,  Strategy,  ReverseSignal,  BacktestEnd }
public enum DecisionType { None,  Open,  Close,  CloseAndReverse }
