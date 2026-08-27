using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8013.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;

namespace StrategyService.Bots.Bot8013;

public sealed class Bot8013OrderEventHandler(
    IOptions<Bot8013Options> options,
    IPositionStore positionStore, 
    IClock clock)
    : TpOnlyGridOrderEventHandler<Bot8013Options>(options.Value, positionStore, clock);
