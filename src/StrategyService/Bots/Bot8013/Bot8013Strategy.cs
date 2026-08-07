using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8013.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;

namespace StrategyService.Bots.Bot8013;

public sealed class Bot8013Strategy(
    IOptions<Bot8013Options> o, 
    TpOnlyGridGapPolicy<Bot8013Options> p)
    :TpOnlyGridStrategy<Bot8013Options>(o.Value, p);

