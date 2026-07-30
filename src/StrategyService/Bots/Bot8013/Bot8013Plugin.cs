using TradingSystem.BotRuntime.Configuration;
using Microsoft.Extensions.Options;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Bots.Bot8013;

public sealed class Bot8013Strategy(
    IOptions<Bot8013Options> o, 
    TpOnlyGridGapPolicy<Bot8013Options> p)
    :TpOnlyGridStrategy<Bot8013Options>(o.Value, p);

public sealed class Bot8013TradeExecutor
    (IOptions<Bot8013Options> o, 
    BinanceTpOnlyPositionService e, 
    IPositionStore s, 
    IClock c,
    IBotRuntimeConfigurationProvider r)
    :TpOnlyGridTradeExecutor<Bot8013Options>(o.Value, e, s, c, r);

public sealed class Bot8013ActivePositionProvider(
    IOptions<Bot8013Options> o, 
    IBinanceFuturesOrderClient orders)
    :TpOnlyGridActivePositionProvider<Bot8013Options>(o.Value, orders);

public sealed class Bot8013OrderEventHandler(
    IOptions<Bot8013Options> o, 
    IPositionStore s, IClock c)
    :TpOnlyGridOrderEventHandler<Bot8013Options>(o.Value, s, c);

public sealed class Bot8013HealingService(
    IOptions<Bot8013Options> o, 
    IPositionStore s, IClock c)
    :TpOnlyGridHealingService<Bot8013Options>(o.Value, s, c);
