using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Positions.Models;
using TradingSystem.Binance.Execution.Models;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public abstract class TpOnlyGridActivePositionProvider<TOptions>(
    TOptions options, 
    IBinanceFuturesOrderClient ordersClient)
    : IBotActivePositionProvider where TOptions : class, 
    ITpOnlyGridBotOptions
{
    public string BotName => options.BotName;
    
    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string symbol, CancellationToken ct)
    {
        if(!symbol.Equals(options.Symbol, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{BotName} does not support '{symbol}'.");
        
        var result = new List<ActivePositionView>();
        
        foreach(var order in await ordersClient.GetOpenOrdersAsync(symbol, ct))
        {
            if(!BinanceClientOrderId.TryParse(order.ClientOrderId, out var bot, out var role, out var id) 
                || !bot.Equals(BotName, StringComparison.OrdinalIgnoreCase) 
                || role != "TP" 
                || order.Price <= 0 
                || !BinanceOrderSide.TryParsePosition(order.PositionSide, out var side))
                continue;
            
            result.Add(new ActivePositionView
            {
                ShortId = id, 
                BotName = BotName, 
                Symbol = symbol, 
                Side = side, 
                TpPrice = order.Price, 
                Quantity = order.Quantity, 
                RemainingQuantity = order.Quantity, 
                CreatedAtUtc = order.UpdateTimeUtc});
        }
        
        return result.OrderByDescending(x => x.CreatedAtUtc).ToArray();
    }
}
