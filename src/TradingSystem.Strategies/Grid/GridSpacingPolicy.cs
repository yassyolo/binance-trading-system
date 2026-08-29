using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Grid.Models;

namespace TradingSystem.Strategies.Grid;

public sealed class GridSpacingPolicy
{
    public PolicyDecision Evaluate(PositionSide side, decimal markPrice, IReadOnlyCollection<GridPositionReference> positions, GridSpacingParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(parameters);

        if (markPrice <= 0)
            return PolicyDecision.Block("Mark price must be greater than zero.");

        if (parameters.SideLimit <= 0)
            return PolicyDecision.Block("ORDER_SIDE_LIMIT must be greater than zero.");

        if (parameters.PriceDistance < 0 || parameters.ProfitDistance < 0)
            return PolicyDecision.Block("Grid distances cannot be negative.");

        var sameSidePositions = positions.Where(p => p.Side == side)
            .OrderByDescending(p => p.OpenedAtUtc)
            .ToArray();

        if (sameSidePositions.Length >= parameters.SideLimit)
            return PolicyDecision.Block($"ORDER_SIDE_LIMIT reached ({sameSidePositions.Length}/{parameters.SideLimit}).");

        if (sameSidePositions.Length == 0)
            return PolicyDecision.Allow("No active TP positions for this side.");

        var newestTakeProfit = sameSidePositions[0].TakeProfitPrice;
        if (newestTakeProfit <= 0)
            return PolicyDecision.Block("Newest position has an invalid take-profit price.");

        if (side == PositionSide.Long)
        {
            var maximumAllowedMark = newestTakeProfit - parameters.ProfitDistance - parameters.PriceDistance;

            return markPrice <= maximumAllowedMark
                ? PolicyDecision.Allow($"LONG spacing valid: mark = {markPrice}, requiredMaximum = {maximumAllowedMark}.")
                : PolicyDecision.Block($"GAP fail LONG: mark = {markPrice}, requiredMaximum = {maximumAllowedMark}.");
        }

        var minimumAllowedMark = newestTakeProfit + parameters.ProfitDistance + parameters.PriceDistance;

        return markPrice >= minimumAllowedMark
            ? PolicyDecision.Allow($"SHORT spacing valid: mark = {markPrice}, requiredMinimum = {minimumAllowedMark}.")
            : PolicyDecision.Block($"GAP fail SHORT: mark = {markPrice}, requiredMinimum = {minimumAllowedMark}.");
    }
}
