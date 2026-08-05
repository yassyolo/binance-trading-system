namespace TradingSystem.Reconciliation.Enums;

public enum HealingActionType 
{ 
    None, 
    DeleteStaleLocalPosition, 
    RestoreLocalPosition, 
    RecreateTakeProfit, 
    RecreateStopLoss, 
    MarkClosed, 
    RequestManualReview 
}

