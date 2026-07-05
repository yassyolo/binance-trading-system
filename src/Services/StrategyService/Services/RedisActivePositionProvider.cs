using TradingSystem.Application.Engine;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Services;

public sealed class RedisActivePositionProvider : IActivePositionProvider
{
    private readonly IPositionStore _positionStore;

    public RedisActivePositionProvider(IPositionStore positionStore)
    {
        _positionStore = positionStore;
    }

    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
        string botName,
        string symbol,
        CancellationToken cancellationToken)
    {
        var positions = await _positionStore.GetAllAsync(botName, cancellationToken);

        return positions
            .Where(x =>
                !x.Closed &&
                x.Status != PositionStatus.Closed &&
                x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .Select(x => new ActivePositionView
            {
                ShortId = x.ShortId,
                BotName = x.BotName,
                Symbol = x.Symbol,
                Side = x.Side,
                Quantity = x.Quantity,
                RemainingQuantity = x.RemainingQuantity,
                EntryPrice = x.EntryPrice,
                TpPrice = x.TpPrice,
                TpClientId = x.TpClientId,
                TpOrderId = x.TpOrderId,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();
    }
}