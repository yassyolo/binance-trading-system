using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Strategies;

public sealed class StrategyDecision
{
    public required StrategyDecisionType DecisionType { get; init; }
    public PositionSide? Side { get; init; }
    public string? Reason { get; init; }

    public IReadOnlyCollection<string> PositionsToClose { get; init; } = [];

    public bool ShouldOpen => DecisionType == StrategyDecisionType.Open;
    public bool ShouldCloseOpposite => PositionsToClose.Count > 0;

    public static StrategyDecision Open(PositionSide side, string reason)
        => new()
        {
            DecisionType = StrategyDecisionType.Open,
            Side = side,
            Reason = reason
        };

    public static StrategyDecision OpenAfterClosing(
        PositionSide side,
        IReadOnlyCollection<string> positionsToClose,
        string reason)
        => new()
        {
            DecisionType = StrategyDecisionType.Open,
            Side = side,
            PositionsToClose = positionsToClose,
            Reason = reason
        };

    public static StrategyDecision Block(string reason)
        => new()
        {
            DecisionType = StrategyDecisionType.Ignore,
            Reason = reason
        };
}