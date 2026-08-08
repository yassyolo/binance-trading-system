using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Positions.Models;

namespace TradingSystem.Strategies.Positions;

public sealed class PositionAdmissionPolicy
{
    public PositionAdmissionDecision Evaluate(PositionSide side, IReadOnlyCollection<(string Id, PositionSide Side)> activePositions, PositionAdmissionParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(activePositions);
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.SideLimit <= 0)
            return new(false, [], "ORDER_SIDE_LIMIT must be greater than zero.");

        if (side == PositionSide.Long && !parameters.EnableLong)
            return new(false, [], "LONG is disabled.");

        if (side == PositionSide.Short && !parameters.EnableShort)
            return new(false, [], "SHORT is disabled.");

        var oppositePositionIds = activePositions.Where(p => p.Side != side)
            .Select(p => p.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (parameters.CloseOppositeFirst && oppositePositionIds.Length > 0)
            return new(true, oppositePositionIds, $"Close {oppositePositionIds.Length} opposite p(s) first.");

        var sameSideCount = activePositions.Count(p => p.Side == side);
        return sameSideCount >= parameters.SideLimit
            ? new(false, [], $"ORDER_SIDE_LIMIT reached ({sameSideCount}/{parameters.SideLimit}).")
            : new(true, [], $"Admission valid. Active same side = {sameSideCount}.");
    }
}
