namespace TradingSystem.Analytics.Models;

public sealed record OptimizationTrial
{
    public required Guid TrialId { get; init; }

    public required Guid OptimizationRunId { get; init; }

    public required int Sequence { get; init; }

    public required string ParametersJson { get; init; }

    public required decimal Score { get; init; }

    public required PerformanceMetricSet Metrics { get; init; }

    public bool Selected { get; init; }
}
