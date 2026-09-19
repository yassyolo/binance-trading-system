using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8015.Configuration;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Positions.Models;
using TradingSystem.Binance.Execution.Models;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Bots.Bot8015;

public sealed class Bot8015ActivePositionProvider(
    IOptions<Bot8015Options> options, 
    IPositionStore positionStore, 
    IBinanceFuturesOrderClient ordersClient):
    IBotActivePositionProvider
{
    readonly Bot8015Options _options = options.Value;
    public string BotName => _options.BotName;
    
    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string symbol, CancellationToken ct)
    {
        var positions = await positionStore.GetAllAsync(BotName, ct);  
        var normal = await ordersClient.GetOpenOrdersAsync(symbol, ct);
        var algo = await ordersClient.GetOpenAlgoOrdersAsync(symbol, ct);
        
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach(var x in normal)
            if(BinanceClientOrderId.TryParse(x.ClientOrderId, out var bot, out _, out var id) && bot == BotName) 
                ids.Add(id);
        
        foreach(var x in algo)
            if(BinanceClientOrderId.TryParse(x.ClientAlgoId, out var bot, out _, out var id) && bot == BotName)
                ids.Add(id);
        
        return positions.Where(x => !x.Closed && x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) 
            && (ids.Contains(x.ShortId) || x.Stop3Pending))
            .Select(x => new ActivePositionView
            {
                ShortId = x.ShortId, 
                BotName = x.BotName, 
                Symbol = x.Symbol, 
                Side = x.Side, 
                Quantity = x.Quantity, 
                RemainingQuantity = x.RemainingQuantity, 
                EntryPrice = x.EntryPrice ?? 0, 
                TpPrice = x.TpPrice, 
                CreatedAtUtc = x.ParentFilledAtUtc ?? x.CreatedAtUtc
            })
            .ToArray();
    }
}
