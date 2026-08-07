using TradingSystem.Operations.Models;

namespace TradingSystem.Operations.Contracts;

public interface IServiceHeartbeatStore 
{ 
    Task UpsertAsync(ServiceHeartbeat heartbeat, CancellationToken ct);
}

