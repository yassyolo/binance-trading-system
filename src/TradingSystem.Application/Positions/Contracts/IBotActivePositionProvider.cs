using TradingSystem.Application.Positions.Models;

namespace TradingSystem.Application.Positions.Contracts;

public interface IBotActivePositionProvider
{
    string BotName { get; }
   
    Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string symbol, CancellationToken ct);
}
