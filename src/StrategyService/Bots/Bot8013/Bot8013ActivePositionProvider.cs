using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8013.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Bots.Bot8013;

public sealed class Bot8013ActivePositionProvider(
    IOptions<Bot8013Options> options,
    IBinanceFuturesOrderClient orders)
    : TpOnlyGridActivePositionProvider<Bot8013Options>(options.Value, orders);