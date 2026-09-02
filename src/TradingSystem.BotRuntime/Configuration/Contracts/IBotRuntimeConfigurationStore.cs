using TradingSystem.BotRuntime.Configuration.Models;

namespace TradingSystem.BotRuntime.Configuration.Contracts;

public interface IBotRuntimeConfigurationStore
{
    Task<BotRuntimeConfiguration?> GetAsync(string botName, CancellationToken ct);
    
    Task<IReadOnlyCollection<BotRuntimeConfiguration>> GetChangedSinceAsync(DateTime changedSinceUtc, CancellationToken ct);
}
