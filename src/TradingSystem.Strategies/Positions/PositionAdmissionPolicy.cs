using TradingSystem.Domain.Enums;

namespace TradingSystem.Strategies.Positions;

public sealed record PositionAdmissionParameters(
    bool EnableLong,
    bool EnableShort,
    int SideLimit,
    bool CloseOppositeFirst);

public sealed record PositionAdmissionDecision(
    bool Allowed,
    IReadOnlyCollection<string> PositionsToClose,
    string Reason);

public sealed class PositionAdmissionPolicy
{
    public PositionAdmissionDecision Evaluate(
        PositionSide side,
        IReadOnlyCollection<(string Id, PositionSide Side)> activePositions,
        PositionAdmissionParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(activePositions);
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.SideLimit <= 0)
            return new(false, [], "ORDER_SIDE_LIMIT must be greater than zero.");

        if (side == PositionSide.Long && !parameters.EnableLong)
            return new(false, [], "LONG is disabled.");

        if (side == PositionSide.Short && !parameters.EnableShort)
            return new(false, [], "SHORT is disabled.");

        var oppositePositionIds = activePositions
            .Where(position => position.Side != side)
            .Select(position => position.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (parameters.CloseOppositeFirst && oppositePositionIds.Length > 0)
        {
            return new(
                true,
                oppositePositionIds,
                $"Close {oppositePositionIds.Length} opposite position(s) first.");
        }

        var sameSideCount = activePositions.Count(position => position.Side == side);
        return sameSideCount >= parameters.SideLimit
            ? new(false, [], $"ORDER_SIDE_LIMIT reached ({sameSideCount}/{parameters.SideLimit}).")
            : new(true, [], $"Admission valid. Active same side = {sameSideCount}.");
    }
}
