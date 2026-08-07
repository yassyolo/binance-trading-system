namespace TradingSystem.Reconciliation.Models;

public sealed record ExchangeStateSnapshot(
    IReadOnlyCollection<ExchangePositionSnapshot> Positions,
    IReadOnlyCollection<ExchangeOrderSnapshot> Orders);
