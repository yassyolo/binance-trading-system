using TradingSystem.Application.Positions.Models;

namespace TradingSystem.Application.Positions.Contracts;

public interface IActivePositionProvider
{
    Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string botName, string symbol, CancellationToken ct);
}
