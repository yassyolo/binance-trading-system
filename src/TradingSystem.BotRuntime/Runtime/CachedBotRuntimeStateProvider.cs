using Microsoft.Extensions.Caching.Memory;
using TradingSystem.BotRuntime.Runtime.Contracts;
using TradingSystem.BotRuntime.Runtime.Models;

namespace TradingSystem.BotRuntime.Runtime;

public sealed class CachedBotRuntimeStateProvider(
    IBotRuntimeStateStore botRuntimeStateStore,  
    IMemoryCache cache) 
    : IBotRuntimeStateProvider
{
    private static string Key(string botName) 
        => $"bot-runtime:{botName.ToUpperInvariant()}";

    public async Task<BotRuntimeState> GetRequiredAsync(string botName, CancellationToken ct)
    {
        if (cache.TryGetValue(Key(botName), out BotRuntimeState? state) && state is not null)
            return state;

        state = await botRuntimeStateStore.GetAsync(botName, ct)
            ?? throw new InvalidOperationException($"Runtime state was not found for bot '{botName}'.");
        
        cache.Set(Key(botName), state, TimeSpan.FromSeconds(15));
        
        return state;
    }

    public void Invalidate(string botName)  
        => cache.Remove(Key(botName));
}
