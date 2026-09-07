using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012OrderEventHandler(
    IOptions<Bot8012Options> options,
    IPositionStore positionStore,
    IClock clock)
    : TpOnlyGridOrderEventHandler<Bot8012Options>(options.Value, positionStore, clock);