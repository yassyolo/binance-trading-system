using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012Strategy(
    IOptions<Bot8012Options> options,
    TpOnlyGridGapPolicy<Bot8012Options> tpGridPolicy)
    : TpOnlyGridStrategy<Bot8012Options>(options.Value, tpGridPolicy);