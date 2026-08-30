using TradingSystem.Domain.Positions;

namespace TradingSystem.Application.Positions.Contracts;

public interface IPositionStore
{
    Task SaveAsync(BotPosition position, CancellationToken ct);
    
    Task<BotPosition?> GetAsync(string botName, string shortId, CancellationToken ct);
   
    Task<IReadOnlyCollection<BotPosition>> GetAllAsync(string botName, CancellationToken ct);
   
    Task DeleteAsync(string botName, string shortId, CancellationToken ct);
}
