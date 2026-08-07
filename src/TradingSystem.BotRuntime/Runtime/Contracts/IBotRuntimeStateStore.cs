using TradingSystem.BotRuntime.Runtime.Models;
using TradingSystem.BotRuntime.Runtime.Models.Enums;

namespace TradingSystem.BotRuntime.Runtime.Contracts;

public interface IBotRuntimeStateStore
{
    Task<BotRuntimeState?> GetAsync(string botName, CancellationToken ct);
    
    Task<IReadOnlyCollection<BotRuntimeState>> GetAllAsync(CancellationToken ct);
    
    Task<BotRuntimeState> TransitionAsync(
        string botName, 
        BotRuntimeStatus status, 
        long expectedVersion, 
        string user, 
        string reason, 
        bool executionEnabled, 
        CancellationToken ct);
}
