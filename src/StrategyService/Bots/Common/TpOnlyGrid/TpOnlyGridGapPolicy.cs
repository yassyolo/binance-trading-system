using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Grid;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public sealed class TpOnlyGridGapPolicy<TOptions>(TOptions options,  GridSpacingPolicy policy)
    where TOptions : class,  ITpOnlyGridBotOptions
{
    public StrategyDecision Evaluate(
        PositionSide side, 
        decimal markPrice, 
        IReadOnlyCollection<ActivePositionView> activePositions, 
        BotRuntimeConfiguration? runtimeConfiguration  =  null)
    {
        var references  =  activePositions
            .Where(x  =>  x.TpPrice is > 0)
            .Select(x  =>  new GridPositionReference(x.Side,  x.TpPrice!.Value,  x.CreatedAtUtc))
            .ToArray();

        var priceDistance  =  runtimeConfiguration?.PriceDistance ?? options.PriceDistance;
        var profitDistance  =  runtimeConfiguration?.ProfitDistance ?? options.ProfitDistance;
        var sideLimit  =  runtimeConfiguration?.OrderSideLimit ?? options.OrderSideLimit;
        var decision  =  policy.Evaluate(side,  markPrice,  references,  new(priceDistance,  profitDistance,  sideLimit));
        return decision.Allowed
            ? StrategyDecision.Open(side,  decision.Reason)
            : StrategyDecision.Block(side,  decision.Reason);
    }
}
