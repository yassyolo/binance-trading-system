namespace TradingSystem.Analytics.Models;

public sealed record PerformanceSnapshot
{
    public required Guid RunId { get; init; }

    public required string BotName { get; init; }

    public required string Symbol { get; init; }

    public required DateTime PeriodFromUtc { get; init; }

    public required DateTime PeriodToUtc { get; init; }

    public required PerformanceMetricSet Metrics { get; init; }

    public decimal Score { get; init; }
}