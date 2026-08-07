namespace TradingSystem.HistoricalDatabase.Models.Enums;

public enum HistoricalEventType
{
    SignalReceived,
    StrategyDecision,
    ExecutionCompleted,
    ProcessingFailed,
    OrderUpdate,
    PositionChanged,
    TradeClosed,
    RuntimeCommand,
    ConfigurationChanged,
    HealingAction,
    ReconciliationFinding,
    PortfolioSnapshot
}
