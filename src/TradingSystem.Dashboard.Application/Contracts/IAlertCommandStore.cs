namespace TradingSystem.Dashboard.Application.Contracts;

public interface IAlertCommandStore 
{ 
    Task AcknowledgeAsync(long alertId, string user, CancellationToken ct);
}

