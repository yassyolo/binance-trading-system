using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class PositionManager(IPositionStore positionStore)
{
    public Task AddAsync(BotPosition position, CancellationToken cancellationToken)
        => positionStore.SaveAsync(position, cancellationToken);

    public Task SaveAsync(BotPosition position, CancellationToken cancellationToken)
        => positionStore.SaveAsync(position, cancellationToken);

    public async Task<IReadOnlyCollection<BotPosition>> GetActivePositionsAsync(string botName, CancellationToken cancellationToken)
    {
        var positions = await positionStore.GetAllAsync(botName, cancellationToken);

        return positions.Where(x => !x.Closed && x.Status != PositionStatus.Closed).ToList();
    }

    public async Task<int> CountBySideAsync(string botName, PositionSide side, CancellationToken cancellationToken)
    {
        var active = await GetActivePositionsAsync(botName, cancellationToken);

        return active.Count(x => x.Side == side);
    }

    public async Task<IReadOnlyCollection<BotPosition>> GetBySideAsync(string botName, PositionSide side, CancellationToken cancellationToken)
    {
        var active = await GetActivePositionsAsync(botName, cancellationToken);

        return active.Where(x => x.Side == side).ToList();
    }

    public Task<IReadOnlyCollection<BotPosition>> GetOppositeAsync(string botName, PositionSide side, CancellationToken cancellationToken)
    {
        var opposite = side == PositionSide.Long
            ? PositionSide.Short
            : PositionSide.Long;

        return GetBySideAsync(botName, opposite, cancellationToken);
    }

    public async Task MarkClosedAsync(BotPosition position, string reason, CancellationToken cancellationToken)
    {
        position.MarkClosed(reason);
        position.ProtectiveActive = false;
        position.TrailingInProgress = false;
        position.Stop3Pending = false;

        await positionStore.SaveAsync(position, cancellationToken);
    }
}