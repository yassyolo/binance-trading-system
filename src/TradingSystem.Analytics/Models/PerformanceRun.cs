using TradingSystem.Analytics.Models.Enums;

namespace TradingSystem.Analytics.Models;

public sealed record PerformanceRun
{
    public required Guid RunId { get; init; }

    public required PerformanceRunType RunType { get; init; }

    public required string BotName { get; init; }

    public required string StrategyVersion { get; init; }

    public required string Symbol { get; init; }

    public required string Interval { get; init; }

    public required DateTime StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public PerformanceRunStatus Status { get; init; }

    public required string ParametersJson { get; init; }

    public string? ParentRunId { get; init; }

    public string? Notes { get; init; }
}
