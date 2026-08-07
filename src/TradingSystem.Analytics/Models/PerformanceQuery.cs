using TradingSystem.Analytics.Models.Enums;

namespace TradingSystem.Analytics.Models;

public sealed record PerformanceQuery
{
    public string? BotName { get; init; }

    public string? Symbol { get; init; }

    public PerformanceRunType? RunType { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public int Take { get; init; } = 100;
}
