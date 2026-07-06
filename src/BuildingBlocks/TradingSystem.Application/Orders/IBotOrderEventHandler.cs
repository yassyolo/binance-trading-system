namespace TradingSystem.Application.Orders;

public interface IBotOrderEventHandler
{
    string BotName { get; }

    Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken);

    Task HandleTpTerminalAsync(
        string shortId,
        string status,
        CancellationToken cancellationToken);

    Task HandleSlTriggeredAsync(
        string shortId,
        CancellationToken cancellationToken);

    Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken);
}
