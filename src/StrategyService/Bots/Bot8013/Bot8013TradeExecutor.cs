using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8013.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.BotRuntime.Configuration.Contracts;

namespace StrategyService.Bots.Bot8013;

public sealed class Bot8013TradeExecutor(
    IOptions<Bot8013Options> options,
    BinanceTpOnlyPositionService tpOnlyPositionService,
    IPositionStore positionStore,
    IClock clock,
    IBotRuntimeConfigurationProvider configProvider)
    : TpOnlyGridTradeExecutor<Bot8013Options>(options.Value, tpOnlyPositionService, positionStore, clock, configProvider);

