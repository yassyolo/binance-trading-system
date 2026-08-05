namespace TradingSystem.Reconciliation.Enums;

public enum ReconciliationFindingType 
{ 
    StaleLocalPosition, 
    MissingLocalPosition, 
    MissingTakeProfit, 
    MissingStopLoss, 
    QuantityMismatch, 
    OrphanExchangePosition, 
    OrphanExchangeOrder 
}

