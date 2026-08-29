using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Positions.Models;
using TradingSystem.Binance.Execution.Models;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012ActivePositionProvider(
    IOptions<Bot8012Options> options, 
    IBinanceFuturesOrderClient orders)
    :IBotActivePositionProvider
{
    private readonly Bot8012Options _options = options.Value;
    
    public string BotName => _options.BotName;
    
    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string symbol, CancellationToken ct)
    {
        var open = await orders.GetOpenOrdersAsync(symbol, ct);
        
        var result = new List<ActivePositionView>();
        
        foreach(var x in open)
        {
            if(!BinanceClientOrderId.TryParse(x.ClientOrderId, out var bot, out var role, out var id) 
                || !bot.Equals(BotName, StringComparison.OrdinalIgnoreCase) 
                || role!= "TP" 
                || x.Price<=0 
                || !BinanceOrderSide.TryParsePosition(x.PositionSide, out var side))
                continue;
            
            result.Add(new ActivePositionView
            {
                ShortId = id, 
                BotName = BotName, 
                Symbol = symbol, 
                Side = side, 
                TpPrice = x.Price, 
                Quantity = x.Quantity, 
                RemainingQuantity = x.Quantity, 
                CreatedAtUtc = x.UpdateTimeUtc});
        }
        
        return result.OrderByDescending(x => x.CreatedAtUtc).ToArray();
    }
}
