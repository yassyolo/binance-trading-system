using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public interface IBotTradeExecutor
{
    string BotName { get; }

    Task OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken cancellationToken);

    Task CloseAsync(
        string botName,
        string shortId,
        string reason,
        CancellationToken cancellationToken);
}