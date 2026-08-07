namespace TradingSystem.EventStore.Constants;

public static class TradingEventTypes
{
    public const string SignalReceived = "trading.signal.received";
    
    public const string SignalRejected = "trading.signal.rejected";
    
    public const string StrategyDecisionTaken = "trading.strategy.decision-taken";
    
    public const string RiskDecisionTaken = "trading.risk.decision-taken";
    
    public const string ExecutionRequested = "trading.execution.requested";
   
    public const string ExecutionCompleted = "trading.execution.completed";
    
    public const string ExecutionFailed = "trading.execution.failed";
    
    public const string PositionOpened = "trading.position.opened";
   
    public const string PositionClosed = "trading.position.closed";
   
    public const string BotRuntimeChanged = "bot.runtime.changed";
   
    public const string BotConfigurationChanged = "bot.configuration.changed";
   
    public const string AlertRaised = "operations.alert.raised";
   
    public const string AlertResolved = "operations.alert.resolved";
   
    public const string JobStatusChanged = "jobs.status.changed";
}
