using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8013;

public sealed class Bot8013GapPolicy(IOptions<Bot8013Options> options)
{
    private readonly Bot8013Options _options = options.Value;

    public StrategyDecision Validate(
        PositionSide side,
        decimal markPrice,
        IReadOnlyCollection<ActivePositionView> activePositions)
    {
        var sameSidePositions = activePositions
            .Where(x => x.Side == side)
            .ToList();

        if (sameSidePositions.Count >= _options.OrderSideLimit)
            return StrategyDecision.Block(
                $"ORDER_SIDE_LIMIT reached ({sameSidePositions.Count}/{_options.OrderSideLimit})");

        if (sameSidePositions.Count == 0)
            return StrategyDecision.Open(side, "OK no active TP");

        var newest = sameSidePositions
            .Where(x => x.TpPrice.HasValue)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

        if (newest?.TpPrice is null)
            return StrategyDecision.Open(side, "OK no valid newest TP");

        var newestTp = Math.Round(newest.TpPrice.Value, 0);
        var mark = Math.Round(markPrice, 0);

        if (side == PositionSide.Long)
        {
            var lastEntry = newestTp - Math.Round(_options.ProfitDistance, 0);
            var requiredMax = lastEntry - Math.Round(_options.PriceDistance, 0);

            if (mark > requiredMax)
                return StrategyDecision.Block(
                    $"GAP fail LONG: mark={mark}, newest_tp={newestTp}, last_entry={lastEntry}, required_max={requiredMax}, gap={Math.Round(_options.PriceDistance, 0)}");
        }
        else
        {
            var lastEntry = newestTp + Math.Round(_options.ProfitDistance, 0);
            var requiredMin = lastEntry + Math.Round(_options.PriceDistance, 0);

            if (mark < requiredMin)
                return StrategyDecision.Block(
                    $"GAP fail SHORT: mark={mark}, newest_tp={newestTp}, last_entry={lastEntry}, required_min={requiredMin}, gap={Math.Round(_options.PriceDistance, 0)}");
        }

        return StrategyDecision.Open(side, $"OK newest_tp={newestTp} mark={mark}");
    }
}