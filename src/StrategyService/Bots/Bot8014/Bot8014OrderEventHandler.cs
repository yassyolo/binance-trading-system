using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8014.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;

namespace StrategyService.Bots.Bot8014;

public sealed class Bot8014OrderEventHandler(
    IOptions<Bot8014Options> options,
    IPositionStore positions,
    IClock clock)
    : TpOnlyGridOrderEventHandler<Bot8014Options>(options.Value, positions, clock);
