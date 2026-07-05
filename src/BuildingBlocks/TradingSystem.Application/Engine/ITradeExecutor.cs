using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Engine;

public interface ITradeExecutor
{
    Task OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        string source,
        CancellationToken cancellationToken);
}