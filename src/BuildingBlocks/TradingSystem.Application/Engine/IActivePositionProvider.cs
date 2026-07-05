using TradingSystem.Application.Strategies;

namespace TradingSystem.Application.Engine;

public interface IActivePositionProvider
{
    Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
        string botName,
        string symbol,
        CancellationToken cancellationToken);
}