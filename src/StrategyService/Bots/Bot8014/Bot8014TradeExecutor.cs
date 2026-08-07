using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8014.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.BotRuntime.Configuration;

namespace StrategyService.Bots.Bot8014;

public sealed class Bot8014TradeExecutor(
    IOptions<Bot8014Options> o,
    BinanceTpOnlyPositionService e,
    IPositionStore s,
    IClock c,
    IBotRuntimeConfigurationProvider r)
    : TpOnlyGridTradeExecutor<Bot8014Options>(o.Value, e, s, c, r);
