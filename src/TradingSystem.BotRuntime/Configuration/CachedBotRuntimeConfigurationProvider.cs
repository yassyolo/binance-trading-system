using System.Collections.Concurrent;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.BotRuntime.Configuration.Models;

namespace TradingSystem.BotRuntime.Configuration;

public sealed class CachedBotRuntimeConfigurationProvider(
    IBotRuntimeConfigurationStore store) 
    : IBotRuntimeConfigurationProvider
{
    private readonly ConcurrentDictionary<string, BotRuntimeConfiguration> _configurations = new(StringComparer.OrdinalIgnoreCase);

    public async Task<BotRuntimeConfiguration?> GetAsync(string botName,  CancellationToken ct)
    {
        if (_configurations.TryGetValue(botName, out var current))
            return current;
       
        var loaded = await store.GetAsync(botName, ct);
        if (loaded is not null) 
            Set(loaded);
        
        return loaded;
    }
    
    public void Set(BotRuntimeConfiguration config)  
        => _configurations.AddOrUpdate(config.BotName, config, 
            (_,  existing) => config.Version >= existing.Version 
            ? config 
            : existing);  
}
