using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Positions.Models;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016ActivePositionProvider(IOptions<Bot8016Options> options, IPositionStore positions) : IBotActivePositionProvider
{
    private readonly Bot8016Options _options = options.Value;
    public string BotName => _options.BotName;

    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string symbol, CancellationToken ct)
    {
        var items = await positions.GetAllAsync(_options.BotName, ct);
        return items
            .Where(x => !x.Closed && x.RemainingQuantity > 0 && x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .Select(x => new ActivePositionView
            {
                ShortId = x.ShortId,
                BotName = x.BotName,
                Symbol = x.Symbol,
                Side = x.Side,
                EntryPrice = x.EntryPrice ?? 0m,
                Quantity = x.Quantity,
                RemainingQuantity = x.RemainingQuantity,
                TpPrice = x.TpPrice,
                CreatedAtUtc = x.ParentFilledAtUtc ?? x.CreatedAtUtc
            })
            .ToArray();
    }
}
