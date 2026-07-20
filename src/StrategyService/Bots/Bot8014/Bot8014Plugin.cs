using TradingSystem.BotRuntime.Configuration;using Microsoft.Extensions.Options;using StrategyService.Bots.Common.TpOnlyGrid;using TradingSystem.Application.Positions;using TradingSystem.Application.Time;using TradingSystem.Binance.Execution;using TradingSystem.Binance.Orders.Contracts;
namespace StrategyService.Bots.Bot8014;
public sealed class Bot8014Strategy(IOptions<Bot8014Options> o, TpOnlyGridGapPolicy<Bot8014Options> p):TpOnlyGridStrategy<Bot8014Options>(o.Value, p);
public sealed class Bot8014TradeExecutor(IOptions<Bot8014Options> o, BinanceTpOnlyPositionService e, IPositionStore s, IClock c, IBotRuntimeConfigurationProvider r):TpOnlyGridTradeExecutor<Bot8014Options>(o.Value, e, s, c, r);
public sealed class Bot8014ActivePositionProvider(IOptions<Bot8014Options> o, IBinanceFuturesOrderClient orders):TpOnlyGridActivePositionProvider<Bot8014Options>(o.Value, orders);
public sealed class Bot8014OrderEventHandler(IOptions<Bot8014Options> o, IPositionStore s, IClock c):TpOnlyGridOrderEventHandler<Bot8014Options>(o.Value, s, c);
public sealed class Bot8014HealingService(IOptions<Bot8014Options> o, IPositionStore s, IClock c):TpOnlyGridHealingService<Bot8014Options>(o.Value, s, c);
