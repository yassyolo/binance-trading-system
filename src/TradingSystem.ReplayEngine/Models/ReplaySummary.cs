namespace TradingSystem.ReplayEngine.Models;

public sealed record ReplaySummary(
    Guid ReplayId,
    long ProcessedEvents,
    long FailedEvents,
    long Signals,
    long StrategyDecisions,
    long RiskAllowed,
    long RiskBlocked,
    long ExecutionsCompleted,
    long ExecutionsFailed,
    long PositionsOpened,
    long PositionsClosed,
    long CandidateMatches,
    long CandidateDifferences,
    string DeterministicHash);
