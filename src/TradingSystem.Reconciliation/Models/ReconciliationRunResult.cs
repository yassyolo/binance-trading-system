namespace TradingSystem.Reconciliation.Models;

public sealed record ReconciliationRunResult(DateTime StartedAtUtc, DateTime CompletedAtUtc, IReadOnlyCollection<ReconciliationFinding> Findings, int HealedCount);

