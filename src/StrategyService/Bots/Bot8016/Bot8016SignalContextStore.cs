using System.Collections.Concurrent;
using StrategyService.Bots.Bot8016.Models;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016SignalContextStore
{
    private readonly ConcurrentDictionary<string, Bot8016EntrySignal> _signals = new(StringComparer.OrdinalIgnoreCase);
    public void Set(string signalId, Bot8016EntrySignal signal) => _signals[signalId] = signal;
    public bool TryGet(string signalId, out Bot8016EntrySignal signal) => _signals.TryGetValue(signalId, out signal!);
    public void Remove(string signalId) => _signals.TryRemove(signalId, out _);
}
