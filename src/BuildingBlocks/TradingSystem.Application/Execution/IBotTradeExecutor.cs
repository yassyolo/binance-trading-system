using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Execution;

public interface IBotTradeExecutor
{
    string BotName { get; }

    Task<TradeExecutionResult> OpenAsync(
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken cancellationToken);

    Task<TradeExecutionResult> CloseAsync(
        string shortId,
        string reason,
        CancellationToken cancellationToken);
}
