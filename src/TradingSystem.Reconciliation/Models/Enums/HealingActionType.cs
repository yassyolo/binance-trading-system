namespace TradingSystem.Reconciliation.Models.Enums;

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

