using TradingSystem.Application.Positions.Models;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Grid;
using TradingSystem.Strategies.Grid.Models;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public sealed class TpOnlyGridGapPolicy<TOptions>(
    TOptions options,  
    GridSpacingPolicy gridPolicy)
    where TOptions : class,  
    ITpOnlyGridBotOptions
{
    public StrategyDecision Evaluate(PositionSide side,  decimal markPrice,  IReadOnlyCollection<ActivePositionView> activePositions,  BotRuntimeConfiguration? runtimeConfiguration  =  null)
    {
        var references = activePositions.Where(x => x.TpPrice is > 0)
            .Select(x => new GridPositionReference(x.Side,  x.TpPrice!.Value,  x.CreatedAtUtc))
            .ToArray();

        var priceDistance = runtimeConfiguration?.PriceDistance ?? options.PriceDistance;
        var profitDistance = runtimeConfiguration?.ProfitDistance ?? options.ProfitDistance;
        var sideLimit  =  runtimeConfiguration?.OrderSideLimit ?? options.OrderSideLimit;
        
        var decision = gridPolicy.Evaluate(side, markPrice, references, new(priceDistance, profitDistance, sideLimit));
        
        return decision.Allowed
            ? StrategyDecision.Open(side, decision.Reason)
            : StrategyDecision.Block(side, decision.Reason);
    }
}
