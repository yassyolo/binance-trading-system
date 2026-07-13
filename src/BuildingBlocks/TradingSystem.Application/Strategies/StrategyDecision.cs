using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Strategies;

public sealed record StrategyDecision
{
    private StrategyDecision(
        bool shouldOpen,
        PositionSide side,
        string reason,
        IReadOnlyCollection<string>? positionsToClose = null)
    {
        ShouldOpen = shouldOpen;
        Side = side;
        Reason = reason;
        PositionsToClose = positionsToClose ?? [];
    }

    public bool ShouldOpen { get; }

    public PositionSide Side { get; }

    public string Reason { get; }

    public IReadOnlyCollection<string> PositionsToClose { get; }

    public bool ShouldClosePositions => PositionsToClose.Count > 0;

    public static StrategyDecision Open(
        PositionSide side,
        string reason)
    {
        return new StrategyDecision(
            shouldOpen: true,
            side,
            reason);
    }

    public static StrategyDecision OpenAfterClosing(
        PositionSide side,
        IReadOnlyCollection<string> positionsToClose,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(positionsToClose);

        if (positionsToClose.Count == 0)
        {
            throw new ArgumentException(
                "At least one position must be provided.",
                nameof(positionsToClose));
        }

        return new StrategyDecision(
            shouldOpen: true,
            side,
            reason,
            positionsToClose);
    }

    public static StrategyDecision Block(
        PositionSide side,
        string reason)
    {
        return new StrategyDecision(
            shouldOpen: false,
            side,
            reason);
    }
}