namespace TradingSystem.Reconciliation.Models;

public sealed record ExchangeOrderSnapshot(
    string Symbol,
    string ClientOrderId,
    string Kind, 
    decimal Quantity,
    decimal? TriggerPrice);
