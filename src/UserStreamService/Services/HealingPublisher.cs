using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Binance.UserStream;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.UserStream;
using TradingSystem.Redis.Messaging;
using UserStreamService.Configuration;

namespace UserStreamService.Services;

public sealed class HealingPublisher(IBinanceOrdersSnapshotProvider snapshots, IRedisMessagePublisher publisher, IOptions<UserStreamServiceOptions> options, TimeProvider time, ILogger<HealingPublisher> logger)
{
    private readonly UserStreamServiceOptions _o = options.Value;private readonly SemaphoreSlim _gate = new(1, 1);private DateTimeOffset? _last;
    public async Task PublishAfterReconnectAsync(TimeSpan downtime, CancellationToken ct)
    {
        if(downtime.TotalSeconds<_o.MinDowntimeForHealingSeconds)
            return;
        await _gate.WaitAsync(ct);
        try
        {
            var now = time.GetUtcNow();
            if(_last is not null && now-_last<TimeSpan.FromSeconds(_o.HealingCooldownSeconds))
                return;
            foreach(var rawSymbol in _o.HealingSymbols.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var symbol = rawSymbol.Trim().ToUpperInvariant();
                var s = await snapshots.GetAsync(symbol, ct);
                var ids = Collect(s.NormalOrders, s.AlgoOrders);
                var msg = new HealingSnapshotMessage{HubTimestampUtc = now.UtcDateTime, SnapshotTimestampUtc = now.UtcDateTime, DowntimeSeconds = Math.Round(downtime.TotalSeconds, 1), Symbol = symbol, NormalOrdersCount = s.NormalOrders.Length, AlgoOrdersCount = s.AlgoOrders.Length, TotalOrdersCount = ids.Count, ActiveClientIds = ids, NormalOrders = s.NormalOrders, AlgoOrders = s.AlgoOrders};
                await publisher.PublishAsync(RedisChannels.Healing, msg, ct);logger.LogInformation("Healing snapshot published. Symbol = {Symbol},  Active = {Count}", symbol, ids.Count);} _last = now;}
        finally
        {
            _gate.Release();
        }
    }
    static IReadOnlyCollection<string> Collect(IEnumerable<JsonElement> normal, IEnumerable<JsonElement> algo)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var x in normal)Add(x, "clientOrderId", set);
        foreach(var x in algo)Add(x, "clientAlgoId", set);
        return set.ToArray();
    }
    static void Add(JsonElement e, string name, ISet<string> set)
    {
        if(e.TryGetProperty(name, out var p) && p.ValueKind==JsonValueKind.String && !string.IsNullOrWhiteSpace(p.GetString()))set.Add(p.GetString()!);
    }
}
