using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8013.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;

namespace StrategyService.Bots.Bot8013;

public sealed class Bot8013HealingService(
    IOptions<Bot8013Options> o,
    IPositionStore s, IClock c)
    : TpOnlyGridHealingService<Bot8013Options>(o.Value, s, c);

