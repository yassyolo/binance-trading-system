using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012ActivePositionProvider(
    IOptions<Bot8012Options> options,
    IBinanceFuturesOrderClient orders)
    : TpOnlyGridActivePositionProvider<Bot8012Options>(options.Value, orders);