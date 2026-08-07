namespace TradingSystem.Reconciliation.Models;

public sealed record ExchangePositionSnapshot(
    string Symbol,
    string Side,
    decimal Quantity,
    decimal EntryPrice);
