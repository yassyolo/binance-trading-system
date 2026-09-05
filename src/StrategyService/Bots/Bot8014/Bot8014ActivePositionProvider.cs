using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8014.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Bots.Bot8014;

public sealed class Bot8014ActivePositionProvider(
    IOptions<Bot8014Options> options,
    IBinanceFuturesOrderClient orders)
    : TpOnlyGridActivePositionProvider<Bot8014Options>(options.Value, orders);
