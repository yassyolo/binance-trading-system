using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class PositionManager(IPositionStore positionStore)
{
    public Task AddAsync(BotPosition position, CancellationToken cancellationToken)
        => positionStore.SaveAsync(position, cancellationToken);

    public async Task<IReadOnlyCollection<BotPosition>> GetActivePositionsAsync(
        string botName,
        CancellationToken cancellationToken)
    {
        var positions = await positionStore.GetAllAsync(botName, cancellationToken);

        return positions
            .Where(x => !x.Closed)
            .ToList();
    }

    public async Task<int> CountBySideAsync(
        string botName,
        PositionSide side,
        CancellationToken cancellationToken)
    {
        var activePositions = await GetActivePositionsAsync(botName, cancellationToken);

        return activePositions.Count(x => x.Side == side);
    }

    public async Task<IReadOnlyCollection<BotPosition>> GetBySideAsync(
        string botName,
        PositionSide side,
        CancellationToken cancellationToken)
    {
        var activePositions = await GetActivePositionsAsync(botName, cancellationToken);

        return activePositions
            .Where(x => x.Side == side)
            .ToList();
    }

    public async Task<IReadOnlyCollection<BotPosition>> GetOppositeAsync(
        string botName,
        PositionSide side,
        CancellationToken cancellationToken)
    {
        var oppositeSide = side == PositionSide.Long
            ? PositionSide.Short
            : PositionSide.Long;

        return await GetBySideAsync(botName, oppositeSide, cancellationToken);
    }

    public async Task MarkClosedAsync(
        BotPosition position,
        CancellationToken cancellationToken)
    {
        position.Closed = true;
        position.Status = "CLOSED";
        position.ClosedAtUtc = DateTime.UtcNow;
        position.UpdatedAtUtc = DateTime.UtcNow;

        await positionStore.SaveAsync(position, cancellationToken);
    }
}