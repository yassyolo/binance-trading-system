using TradingSystem.Application.Positions.Models;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.PaperTrading.Models.Enums;

namespace TradingSystem.PaperTrading.Position;

public sealed class PaperActivePositionProvider(IPaperTradingStore store)
{
    public async Task<IReadOnlyCollection<ActivePositionView>> GetAsync(string botName, string symbol, CancellationToken ct)
    {
        var positions = await store.QueryAsync(botName, symbol, PaperPositionStatus.Open, 0, 500, ct);
       
        return positions.Select(x => new ActivePositionView
        {
            ShortId = x.ShortId,
            BotName = x.BotName,
            Symbol = x.Symbol,
            Side = x.Side,
            EntryPrice = x.EntryPrice,
            Quantity = x.Quantity,
            RemainingQuantity = x.Quantity,
            TpPrice = x.TakeProfitPrice,
            CreatedAtUtc = x.OpenedAtUtc
        }).ToArray();
    }
}