using System.Collections.Concurrent;
using TradingSystem.BotRuntime.Configuration.Contracts;

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
   
    public BotRuntimeConfiguration? GetCurrent(string botName)  
        => _configurations.GetValueOrDefault(botName);
    
    public void Set(BotRuntimeConfiguration config)  
        => _configurations.AddOrUpdate(config.BotName,  config, 
            (_,  existing) => config.Version >= existing.Version ? config : existing);
    
    public void Invalidate(string botName)  
        => _configurations.TryRemove(botName,  out _);
}
