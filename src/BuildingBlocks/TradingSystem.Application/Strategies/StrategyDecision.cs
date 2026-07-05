using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Strategies;

public sealed class StrategyDecision
{
    public required StrategyDecisionType DecisionType { get; init; }
    public PositionSide? Side { get; init; }
    public string? Reason { get; init; }

    public bool ShouldOpen => DecisionType == StrategyDecisionType.Open;

    public static StrategyDecision Open(PositionSide side, string reason)
        => new()
        {
            DecisionType = StrategyDecisionType.Open,
            Side = side,
            Reason = reason
        };

    public static StrategyDecision Block(string reason)
        => new()
        {
            DecisionType = StrategyDecisionType.Ignore,
            Reason = reason
        };
}