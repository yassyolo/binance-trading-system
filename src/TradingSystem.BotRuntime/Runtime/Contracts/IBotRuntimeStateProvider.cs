using TradingSystem.BotRuntime.Runtime.Models;

namespace TradingSystem.BotRuntime.Runtime.Contracts;

public interface IBotRuntimeStateProvider
{
    Task<BotRuntimeState> GetRequiredAsync(string botName, CancellationToken ct);
    
    void Invalidate(string botName);
}
