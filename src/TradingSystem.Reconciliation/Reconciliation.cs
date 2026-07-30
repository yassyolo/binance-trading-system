using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Domain.Positions;

namespace TradingSystem.Reconciliation;

public enum ReconciliationSeverity { Information,  Warning,  Critical }
public enum ReconciliationFindingType { StaleLocalPosition,  MissingLocalPosition,  MissingTakeProfit,  MissingStopLoss,  QuantityMismatch,  OrphanExchangePosition,  OrphanExchangeOrder }
public enum HealingActionType { None,  DeleteStaleLocalPosition,  RestoreLocalPosition,  RecreateTakeProfit,  RecreateStopLoss,  MarkClosed,  RequestManualReview }

public sealed record ExchangePositionSnapshot(string Symbol,  string Side,  decimal Quantity,  decimal EntryPrice);
public sealed record ExchangeOrderSnapshot(string Symbol,  string ClientOrderId,  string Kind,  decimal Quantity,  decimal? TriggerPrice);
public sealed record ExchangeStateSnapshot(IReadOnlyCollection<ExchangePositionSnapshot> Positions,  IReadOnlyCollection<ExchangeOrderSnapshot> Orders);
public sealed record ReconciliationFinding(Guid Id,  DateTime DetectedAtUtc,  string BotName,  string Symbol,  string? ShortId, 
    ReconciliationFindingType Type,  ReconciliationSeverity Severity,  string Details,  HealingActionType SuggestedAction,  bool AutoHealAllowed);
public sealed record ReconciliationRunResult(DateTime StartedAtUtc,  DateTime CompletedAtUtc,  IReadOnlyCollection<ReconciliationFinding> Findings,  int HealedCount);

public interface IExchangeStateProvider { Task<ExchangeStateSnapshot> GetAsync(string symbol,  CancellationToken ct); }
public interface IReconciliationFindingStore
{
    Task SaveRunAsync(ReconciliationRunResult result,  CancellationToken ct);
    Task<bool> HasUnresolvedCriticalAsync(CancellationToken ct);
}
public interface IHealingActionExecutor { Task<bool> ExecuteAsync(ReconciliationFinding finding,  CancellationToken ct); }
