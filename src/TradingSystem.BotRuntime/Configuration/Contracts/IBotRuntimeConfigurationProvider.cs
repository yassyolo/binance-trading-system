using TradingSystem.BotRuntime.Configuration.Models;

namespace TradingSystem.BotRuntime.Configuration.Contracts;

public interface IBotRuntimeConfigurationProvider
{
    Task<BotRuntimeConfiguration?> GetAsync(string botName, CancellationToken ct);
        
    void Set(BotRuntimeConfiguration configuration);
   }
