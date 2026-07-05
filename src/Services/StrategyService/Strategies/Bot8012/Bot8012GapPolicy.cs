using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8012;

public sealed class Bot8012GapPolicy
{
    private readonly Bot8012Options _options;

    public Bot8012GapPolicy(IOptions<Bot8012Options> options)
    {
        _options = options.Value;
    }

    public StrategyDecision Validate(
        PositionSide side,
        decimal markPrice,
        IReadOnlyCollection<ActivePositionView> activePositions)
    {
        var sameSidePositions = activePositions
            .Where(x => x.Side == side)
            .ToList();

        if (sameSidePositions.Count >= _options.OrderSideLimit)
        {
            return StrategyDecision.Block(
                $"ORDER_SIDE_LIMIT reached ({sameSidePositions.Count}/{_options.OrderSideLimit})");
        }

        if (sameSidePositions.Count == 0)
            return StrategyDecision.Open(side, "OK no active TP positions");

        var newest = sameSidePositions
            .Where(x => x.TpPrice.HasValue)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

        if (newest is null || newest.TpPrice is null)
            return StrategyDecision.Open(side, "OK no valid newest TP");

        var newestTp = Math.Round(newest.TpPrice.Value, 0);
        var mark = Math.Round(markPrice, 0);

        if (side == PositionSide.Long)
        {
            var lastEntry = newestTp - Math.Round(_options.ProfitDistance, 0);
            var requiredMax = lastEntry - Math.Round(_options.PriceDistance, 0);

            if (mark > requiredMax)
            {
                return StrategyDecision.Block(
                    $"GAP fail LONG: mark={mark}, newest_tp={newestTp}, last_entry={lastEntry}, required_max={requiredMax}");
            }
        }
        else
        {
            var lastEntry = newestTp + Math.Round(_options.ProfitDistance, 0);
            var requiredMin = lastEntry + Math.Round(_options.PriceDistance, 0);

            if (mark < requiredMin)
            {
                return StrategyDecision.Block(
                    $"GAP fail SHORT: mark={mark}, newest_tp={newestTp}, last_entry={lastEntry}, required_min={requiredMin}");
            }
        }

        return StrategyDecision.Open(side, $"OK newest_tp={newestTp} mark={mark}");
    }
}