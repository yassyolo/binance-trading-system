namespace TradingSystem.Dashboard.Contracts.Models.Analytics;

public sealed record StrategyComparisonDto(Guid LeftRunId, Guid RightRunId, string LeftLabel, string RightLabel, decimal NetProfitDifference, decimal DrawdownDifference, decimal WinRateDifference, decimal ScoreDifference);

