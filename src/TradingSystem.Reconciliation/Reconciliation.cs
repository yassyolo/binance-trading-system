using TradingSystem.Reconciliation.Enums;

namespace TradingSystem.Reconciliation;

public sealed record ExchangePositionSnapshot(
    string Symbol,  
    string Side,  
    decimal Quantity,  
    decimal EntryPrice);
public sealed record ExchangeOrderSnapshot(
    string Symbol,  
    string ClientOrderId,  
    string Kind,  decimal Quantity,  
    decimal? TriggerPrice);
public sealed record ExchangeStateSnapshot(
    IReadOnlyCollection<ExchangePositionSnapshot> Positions,  
    IReadOnlyCollection<ExchangeOrderSnapshot> Orders);
public sealed record ReconciliationFinding(
    Guid Id,  
    DateTime DetectedAtUtc,  
    string BotName,  string Symbol,  
    string? ShortId, 
    ReconciliationFindingType Type,  
    ReconciliationSeverity Severity,  
    string Details,  HealingActionType SuggestedAction,  bool AutoHealAllowed);
public sealed record ReconciliationRunResult(DateTime StartedAtUtc,  DateTime CompletedAtUtc,  IReadOnlyCollection<ReconciliationFinding> Findings,  int HealedCount);

public interface IExchangeStateProvider 
{ 
    Task<ExchangeStateSnapshot> GetAsync(string symbol,  CancellationToken ct); }
public interface IReconciliationFindingStore
{
    Task SaveRunAsync(ReconciliationRunResult result,  CancellationToken ct);
    Task<bool> HasUnresolvedCriticalAsync(CancellationToken ct);
}
