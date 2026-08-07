namespace TradingSystem.Reconciliation.Models.Enums;

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

