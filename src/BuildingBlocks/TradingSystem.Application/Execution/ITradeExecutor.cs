using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Execution;

public interface ITradeExecutor
{
    Task<TradeExecutionResult> OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken cancellationToken);

    Task<TradeExecutionResult> CloseAsync(
        string botName,
        string shortId,
        string reason,
        CancellationToken cancellationToken);
}
