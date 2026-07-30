namespace TradingSystem.Application.Positions;

public interface IActivePositionProvider
{
    Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string botName,  string symbol,  CancellationToken ct);
}
