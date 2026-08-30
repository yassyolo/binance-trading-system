namespace TradingSystem.Application.Orders;

public interface IBotOrderEventHandler
{
    string BotName { get; }
    
    Task HandleTpFilledAsync(string shortId, decimal executedQuantity, CancellationToken ct);
    
    Task HandleTpTerminalAsync(string shortId, string status, CancellationToken ct);
   
    Task HandleSlTriggeredAsync(string shortId, CancellationToken ct);
   
    Task HandleStop3TriggeredAsync(string shortId, CancellationToken ct);
}
