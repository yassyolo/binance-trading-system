using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8013;

public sealed class Bot8013GapPolicy(
    IOptions<Bot8013Options> options)
{
    private readonly Bot8013Options _options = options.Value;

    public StrategyDecision Validate(
        PositionSide side,
        decimal markPrice,
        IReadOnlyCollection<ActivePositionView> activePositions)
    {
        var sameSidePositions = activePositions
            .Where(x => x.Side == side)
            .ToArray();

        if (sameSidePositions.Length >= _options.OrderSideLimit)
        {
            return StrategyDecision.Block(
                side,
                $"ORDER_SIDE_LIMIT reached ({sameSidePositions.Length}/{_options.OrderSideLimit}).");
        }

        if (sameSidePositions.Length == 0)
        {
            return StrategyDecision.Open(
                side,
                "No active TP positions for this side.");
        }

        var newest = sameSidePositions
            .Where(x => x.TpPrice is > 0)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

        if (newest?.TpPrice is null)
        {
            return StrategyDecision.Block(
                side,
                "Active positions exist, but no valid TP price was found.");
        }

        // Python round(float(x)) uses banker's rounding, matching ToEven.
        var newestTp = RoundForPythonParity(newest.TpPrice.Value);
        var mark = RoundForPythonParity(markPrice);
        var profitDistance = RoundForPythonParity(_options.ProfitDistance);
        var priceDistance = RoundForPythonParity(_options.PriceDistance);

        if (side == PositionSide.Long)
        {
            var lastEntry = RoundForPythonParity(newestTp - profitDistance);
            var requiredMaximum = lastEntry - priceDistance;

            if (mark > requiredMaximum)
            {
                return StrategyDecision.Block(
                    side,
                    $"GAP fail LONG: mark={mark}, newestTp={newestTp}, " +
                    $"lastEntry={lastEntry}, requiredMaximum={requiredMaximum}, gap={priceDistance}.");
            }

            return StrategyDecision.Open(
                side,
                $"LONG spacing valid: mark={mark}, newestTp={newestTp}, " +
                $"lastEntry={lastEntry}, requiredMaximum={requiredMaximum}.");
        }

        var shortLastEntry = RoundForPythonParity(newestTp + profitDistance);
        var requiredMinimum = shortLastEntry + priceDistance;

        if (mark < requiredMinimum)
        {
            return StrategyDecision.Block(
                side,
                $"GAP fail SHORT: mark={mark}, newestTp={newestTp}, " +
                $"lastEntry={shortLastEntry}, requiredMinimum={requiredMinimum}, gap={priceDistance}.");
        }

        return StrategyDecision.Open(
            side,
            $"SHORT spacing valid: mark={mark}, newestTp={newestTp}, " +
            $"lastEntry={shortLastEntry}, requiredMinimum={requiredMinimum}.");
    }

    private static decimal RoundForPythonParity(decimal value)
        => Math.Round(value, 0, MidpointRounding.ToEven);
}
