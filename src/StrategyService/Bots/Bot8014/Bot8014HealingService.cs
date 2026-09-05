using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8014.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;

namespace StrategyService.Bots.Bot8014;

public sealed class Bot8014HealingService(
    IOptions<Bot8014Options> options,
    IPositionStore positionStore,
    IClock clock)
    : TpOnlyGridHealingService<Bot8014Options>(options.Value, positionStore, clock);
