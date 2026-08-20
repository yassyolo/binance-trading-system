namespace TradingSystem.Reconciliation.Models;

public sealed record ReconciliationRunResult(
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    IReadOnlyCollection<ReconciliationFinding> Findings,
    int HealedCount)
{
    public IReadOnlyCollection<string> EvaluatedSymbols { get; init; } = Array.Empty<string>();
}
