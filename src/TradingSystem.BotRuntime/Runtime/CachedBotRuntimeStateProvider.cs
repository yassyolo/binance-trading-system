using Microsoft.Extensions.Caching.Memory;

namespace TradingSystem.BotRuntime.Runtime;

public sealed class CachedBotRuntimeStateProvider(IBotRuntimeStateStore store,  IMemoryCache cache) : IBotRuntimeStateProvider
{
    private static string Key(string botName)  =>  $"bot-runtime:{botName.ToUpperInvariant()}";

    public async Task<BotRuntimeState> GetRequiredAsync(string botName,  CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(Key(botName),  out BotRuntimeState? state)  &&  state is not null)
            return state;

        state  =  await store.GetAsync(botName,  cancellationToken)
            ?? throw new InvalidOperationException($"Runtime state was not found for bot '{botName}'.");
        cache.Set(Key(botName),  state,  TimeSpan.FromSeconds(15));
        return state;
    }

    public void Invalidate(string botName)  =>  cache.Remove(Key(botName));
}
