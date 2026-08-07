using TradingSystem.EventStore;
using TradingSystem.EventStore.Models;
using TradingSystem.ReplayEngine.Models;

namespace TradingSystem.ReplayEngine.Contracts;

public interface IReplayEventSource
{
    Task<IReadOnlyList<StoredTradingEvent>> ReadForwardAsync(CreateReplayRequest request, long afterGlobalPosition, int take, CancellationToken ct);
    
    Task<long> CountAsync(CreateReplayRequest request, CancellationToken ct);
}
