using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Contracts.Klines;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Infrastructure.Serialization;

namespace MarketDataService;

public sealed class KlinePublisher(
    IConnectionMultiplexer redis, 
    ILogger<KlinePublisher> logger)
{
    readonly IDatabase db = redis.GetDatabase();
    readonly ISubscriber sub = redis.GetSubscriber();
    
    public async Task PublishAsync(JsonElement k, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if(!S(k, "s", out var symbol) || !S(k, "i", out var interval) || !L(k, "t", out var ot) || !L(k, "T", out var ctms))
            return;
        symbol = symbol.ToUpperInvariant();
        interval = interval.ToLowerInvariant();
        var m = new ClosedKlineMessage(symbol, ot, O(k, "o"), O(k, "h"), O(k, "l"), O(k, "c"), O(k, "v"), ctms, interval);
        var json = JsonSerializer.Serialize(m, JsonDefaults.Messaging);
        await db.StringSetAsync($"kline:{symbol.ToLowerInvariant()}:{interval}", json);
        await sub.PublishAsync(RedisChannel.Literal(RedisChannels.Kline(interval, symbol)), json);
        logger.LogInformation("Closed kline published. Symbol = {Symbol},  Interval = {Interval}", symbol, interval);
    }
    static bool S(JsonElement e, string n, out string v)
    {
        v = e.TryGetProperty(n, out var p)?p.GetString()??"":"";return v.Length>0;
    }
    static bool L(JsonElement e, string n, out long v)
    {
        v = 0;
        return e.TryGetProperty(n, out var p) && p.TryGetInt64(out v);
    }
    static string? O(JsonElement e, string n)
         => e.TryGetProperty(n, out var p)?p.ToString():null;
}
