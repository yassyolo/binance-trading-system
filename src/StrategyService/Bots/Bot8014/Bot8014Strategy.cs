using Microsoft.Extensions.Options;
using StrategyService.Bots.Common.TpOnlyGrid;
using StrategyService.Bots.Bot8014.Configuration;

namespace StrategyService.Bots.Bot8014;

public sealed class Bot8014Strategy(
    IOptions<Bot8014Options> o, 
    TpOnlyGridGapPolicy<Bot8014Options> p)
    :TpOnlyGridStrategy<Bot8014Options>(o.Value, p);
