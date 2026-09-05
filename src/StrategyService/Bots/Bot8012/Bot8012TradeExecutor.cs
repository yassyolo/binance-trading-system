using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.BotRuntime.Configuration.Contracts;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012TradeExecutor(
    IOptions<Bot8012Options> options,
    BinanceTpOnlyPositionService tpOnlyPositionService,
    IPositionStore positionStore,
    IClock clock,
    IBotRuntimeConfigurationProvider configProvider)
    : TpOnlyGridTradeExecutor<Bot8012Options>(options.Value, tpOnlyPositionService, positionStore, clock, configProvider);