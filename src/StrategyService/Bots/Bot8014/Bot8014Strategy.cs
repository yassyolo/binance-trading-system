using Microsoft.Extensions.Options;
using StrategyService.Bots.Common.TpOnlyGrid;
using StrategyService.Bots.Bot8014.Configuration;

namespace StrategyService.Bots.Bot8014;

public sealed class Bot8014Strategy(
    IOptions<Bot8014Options> options, 
    TpOnlyGridGapPolicy<Bot8014Options> tpGridPolicy)
    :TpOnlyGridStrategy<Bot8014Options>(options.Value, tpGridPolicy);
