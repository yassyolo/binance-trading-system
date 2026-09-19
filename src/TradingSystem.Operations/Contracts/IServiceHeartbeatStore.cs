using TradingSystem.Operations.Models;

namespace TradingSystem.Operations.Contracts;

public interface IServiceHeartbeatStore 
{ 
    Task UpsertHeartbeatAsync(ServiceHeartbeat heartbeat, CancellationToken ct);
}

