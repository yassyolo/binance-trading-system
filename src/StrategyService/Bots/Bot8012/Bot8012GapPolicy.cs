using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using TradingSystem.Application.Positions.Models;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Grid;
using TradingSystem.Strategies.Grid.Models;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012GapPolicy(IOptions<Bot8012Options> options,  GridSpacingPolicy policy)
{
    private readonly Bot8012Options _options  =  options.Value;

    public StrategyDecision Evaluate(PositionSide side,  decimal markPrice,  IReadOnlyCollection<ActivePositionView> activePositions,  BotRuntimeConfiguration? runtimeConfiguration  =  null)
    {
        var references  =  activePositions.Where(x  =>  x.TpPrice is > 0)
            .Select(x  =>  new GridPositionReference(x.Side,  x.TpPrice!.Value,  x.CreatedAtUtc))
            .ToArray();
       
        var decision  =  policy.Evaluate(side,  markPrice,  references,  new(
            runtimeConfiguration?.PriceDistance ?? _options.PriceDistance, 
            runtimeConfiguration?.ProfitDistance ?? _options.ProfitDistance, 
            runtimeConfiguration?.OrderSideLimit ?? _options.OrderSideLimit));
        
        return decision.Allowed ? StrategyDecision.Open(side,  decision.Reason) : StrategyDecision.Block(side,  decision.Reason);
    }
}
