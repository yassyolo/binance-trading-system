using System.Collections.Concurrent;

namespace StrategyService.Services;

public sealed class OrderEventDeduplicationService
{
    private readonly ConcurrentDictionary<string, DateTime> _seenEvents = new();

    public bool IsDuplicate(string eventKey, TimeSpan ttl)
    {
        var now = DateTime.UtcNow;

        Cleanup(now, ttl);

        if (_seenEvents.TryGetValue(eventKey, out var seenAt) &&
            now - seenAt < ttl)
        {
            return true;
        }

        _seenEvents[eventKey] = now;

        return false;
    }

    private void Cleanup(DateTime now, TimeSpan ttl)
    {
        if (_seenEvents.Count < 5_000)
            return;

        foreach (var item in _seenEvents)
        {
            if (now - item.Value > ttl)
                _seenEvents.TryRemove(item.Key, out _);
        }
    }
}