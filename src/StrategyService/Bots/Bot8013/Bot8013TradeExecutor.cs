using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8013.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.BotRuntime.Configuration;

namespace StrategyService.Bots.Bot8013;

public sealed class Bot8013TradeExecutor
    (IOptions<Bot8013Options> o,
    BinanceTpOnlyPositionService e,
    IPositionStore s,
    IClock c,
    IBotRuntimeConfigurationProvider r)
    : TpOnlyGridTradeExecutor<Bot8013Options>(o.Value, e, s, c, r);

