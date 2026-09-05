using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Positions.Models;
using TradingSystem.Binance.Execution.Models;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public abstract class TpOnlyGridActivePositionProvider<TOptions>(
    TOptions options, 
    IBinanceFuturesOrderClient orders)
    : IBotActivePositionProvider where TOptions : class, ITpOnlyGridBotOptions
{
    public string BotName => options.BotName;
    
    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string symbol, CancellationToken ct)
    {
        if(!symbol.Equals(options.Symbol, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{BotName} does not support '{symbol}'.");
        
        var result = new List<ActivePositionView>();
        
        foreach(var o in await orders.GetOpenOrdersAsync(symbol, ct))
        {
            if(!BinanceClientOrderId.TryParse(o.ClientOrderId, out var bot, out var role, out var id) 
                || !bot.Equals(BotName, StringComparison.OrdinalIgnoreCase) 
                || role != "TP" 
                || o.Price <= 0 
                || !BinanceOrderSide.TryParsePosition(o.PositionSide, out var side))
                continue;
            
            result.Add(new ActivePositionView
            {
                ShortId = id, 
                BotName = BotName, 
                Symbol = symbol, 
                Side = side, 
                TpPrice = o.Price, 
                Quantity = o.Quantity, 
                RemainingQuantity = o.Quantity, 
                CreatedAtUtc = o.UpdateTimeUtc});
        }
        
        return result.OrderByDescending(x => x.CreatedAtUtc).ToArray();
    }
}
