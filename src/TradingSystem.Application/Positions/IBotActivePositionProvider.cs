namespace TradingSystem.Application.Positions;

public interface IBotActivePositionProvider
{
    string BotName {  get;  }
    Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string symbol,  CancellationToken cancellationToken);
}
