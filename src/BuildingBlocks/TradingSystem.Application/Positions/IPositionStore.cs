using TradingSystem.Domain.Positions;

namespace TradingSystem.Application.Positions;

public interface IPositionStore
{
    Task SaveAsync(BotPosition position, CancellationToken cancellationToken);
    Task<BotPosition?> GetAsync(string botName, string shortId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BotPosition>> GetAllAsync(string botName, CancellationToken cancellationToken);
    Task DeleteAsync(string botName, string shortId, CancellationToken cancellationToken);
}