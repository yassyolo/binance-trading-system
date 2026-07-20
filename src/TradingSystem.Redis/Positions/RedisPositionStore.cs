using System.Globalization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace TradingSystem.Redis.Positions;

public sealed class RedisPositionStore(IConnectionMultiplexer redis,  RedisKeyFactory keys,  ILogger<RedisPositionStore> logger) : IPositionStore
{
    private readonly IDatabase _db = redis.GetDatabase();
    public async Task SaveAsync(BotPosition p, CancellationToken ct){ct.ThrowIfCancellationRequested();await _db.HashSetAsync(keys.Position(p.BotName, p.ShortId), ToEntries(p));}
    public async Task<BotPosition?> GetAsync(string bot, string id, CancellationToken ct){ct.ThrowIfCancellationRequested();var e = await _db.HashGetAllAsync(keys.Position(bot, id));return e.Length==0?null:FromEntries(e);}
    public async Task<IReadOnlyCollection<BotPosition>> GetAllAsync(string bot, CancellationToken ct)
    {
        var result = new List<BotPosition>();
        foreach(var endpoint in redis.GetEndPoints())
        {
            IServer server; try{server = redis.GetServer(endpoint);}catch(Exception ex){logger.LogWarning(ex, "Could not access Redis endpoint {Endpoint}", endpoint);continue;}
            if(!server.IsConnected) continue;
            await foreach(var key in server.KeysAsync(pattern:keys.PositionPattern(bot)))
            {ct.ThrowIfCancellationRequested();var e = await _db.HashGetAllAsync(key);if(e.Length>0)result.Add(FromEntries(e));}
        }
        return result.GroupBy(x => x.ShortId, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToArray();
    }
    public Task DeleteAsync(string bot, string id, CancellationToken ct){ct.ThrowIfCancellationRequested();return _db.KeyDeleteAsync(keys.Position(bot, id));}
    private static HashEntry[] ToEntries(BotPosition p) => 
    [
        new("short_id", p.ShortId), new("bot_name", p.BotName), new("symbol", p.Symbol), new("side", p.Side.ToString()), new("mode", p.Mode.ToString()), 
        new("quantity", D(p.Quantity)), new("remaining_quantity", D(p.RemainingQuantity)), new("entry_price", D(p.EntryPrice)), 
        new("parent_client_id", S(p.ParentClientId)), new("parent_order_id", S(p.ParentOrderId)), 
        new("tp_client_id", S(p.TpClientId)), new("tp_order_id", S(p.TpOrderId)), new("tp_price", D(p.TpPrice)), new("tp_status", S(p.TpStatus)), new("tp_executed", B(p.TpExecuted)), 
        new("sl_client_id", S(p.SlClientId)), new("sl_order_id", S(p.SlOrderId)), new("sl_price", D(p.SlPrice)), new("sl_status", S(p.SlStatus)), new("sl_executed", B(p.SlExecuted)), 
        new("stop3_client_id", S(p.Stop3ClientId)), new("stop3_order_id", S(p.Stop3OrderId)), new("stop3_current", D(p.Stop3Current)), new("stop3_initial", D(p.Stop3Initial)), new("stop3_previous", D(p.Stop3Previous)), new("stop3_new_pending", D(p.Stop3NewPending)), new("stop3_status", S(p.Stop3Status)), new("stop3_created", B(p.Stop3Created)), new("stop3_pending", B(p.Stop3Pending)), new("trail_count", p.TrailCount), new("trailing_in_progress", B(p.TrailingInProgress)), 
        new("close_client_id", S(p.CloseClientId)), new("close_order_id", S(p.CloseOrderId)), new("close_status", S(p.CloseStatus)), new("protective_active", B(p.ProtectiveActive)), new("manual_position", B(p.ManualPosition)), new("status", p.Status.ToString()), new("source", S(p.Source)), new("created_at", T(p.CreatedAtUtc)), new("updated_at", T(p.UpdatedAtUtc)), new("parent_filled_at", T(p.ParentFilledAtUtc)), new("tp_filled_at", T(p.TpFilledAtUtc)), new("sl_triggered_at", T(p.SlTriggeredAtUtc)), new("stop3_triggered_at", T(p.Stop3TriggeredAtUtc)), new("closed_at", T(p.ClosedAtUtc)), 
        new("signal_candle_high", D(p.SignalCandleHigh)), new("signal_candle_low", D(p.SignalCandleLow)), new("signal_candle_close_time", p.SignalCandleCloseTime?.ToString(CultureInfo.InvariantCulture)??""), new("high_reached", B(p.HighReached))
    ];
    private static BotPosition FromEntries(HashEntry[] entries)
    {
        var m = entries.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
        var p = new BotPosition{ShortId = G(m, "short_id"), BotName = G(m, "bot_name"), Symbol = G(m, "symbol"), Side = E(m, "side", PositionSide.Long), Mode = E(m, "mode", PositionMode.TpOnly), Quantity = Dec(m, "quantity")??0, RemainingQuantity = Dec(m, "remaining_quantity")??0, EntryPrice = Dec(m, "entry_price"), ParentClientId = N(m, "parent_client_id"), ParentOrderId = N(m, "parent_order_id"), TpClientId = N(m, "tp_client_id"), TpOrderId = N(m, "tp_order_id"), TpPrice = Dec(m, "tp_price"), TpStatus = N(m, "tp_status"), TpExecuted = Bool(m, "tp_executed"), SlClientId = N(m, "sl_client_id"), SlOrderId = N(m, "sl_order_id"), SlPrice = Dec(m, "sl_price"), SlStatus = N(m, "sl_status"), SlExecuted = Bool(m, "sl_executed"), Stop3ClientId = N(m, "stop3_client_id"), Stop3OrderId = N(m, "stop3_order_id"), Stop3Current = Dec(m, "stop3_current"), Stop3Initial = Dec(m, "stop3_initial"), Stop3Previous = Dec(m, "stop3_previous"), Stop3NewPending = Dec(m, "stop3_new_pending"), Stop3Status = N(m, "stop3_status"), Stop3Created = Bool(m, "stop3_created"), Stop3Pending = Bool(m, "stop3_pending"), TrailCount = Int(m, "trail_count"), TrailingInProgress = Bool(m, "trailing_in_progress"), CloseClientId = N(m, "close_client_id"), CloseOrderId = N(m, "close_order_id"), CloseStatus = N(m, "close_status"), ProtectiveActive = Bool(m, "protective_active"), ManualPosition = Bool(m, "manual_position"), Source = N(m, "source"), CreatedAtUtc = Dt(m, "created_at")??DateTime.UtcNow, SignalCandleHigh = Dec(m, "signal_candle_high"), SignalCandleLow = Dec(m, "signal_candle_low"), SignalCandleCloseTime = Long(m, "signal_candle_close_time"), HighReached = Bool(m, "high_reached")};
        p.RestoreStatus(E(m, "status", PositionStatus.New), Dt(m, "updated_at"), Dt(m, "parent_filled_at"), Dt(m, "tp_filled_at"), Dt(m, "sl_triggered_at"), Dt(m, "stop3_triggered_at"), Dt(m, "closed_at")); return p;
    }
    static string S(string? v) => v??""; static string D(decimal? v) => v?.ToString(CultureInfo.InvariantCulture)??"";static string D(decimal v) => v.ToString(CultureInfo.InvariantCulture);static string B(bool v) => v?"true":"false";static string T(DateTime? v) => v?.ToString("O", CultureInfo.InvariantCulture)??"";static string T(DateTime v) => v.ToString("O", CultureInfo.InvariantCulture);
    static string G(Dictionary<string, string>m, string k, string d = "") => m.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v)?v:d;static string? N(Dictionary<string, string>m, string k) => m.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v)?v:null;
    static decimal? Dec(Dictionary<string, string>m, string k) => decimal.TryParse(G(m, k), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)?v:null;static int Int(Dictionary<string, string>m, string k) => int.TryParse(G(m, k), out var v)?v:0;static long? Long(Dictionary<string, string>m, string k) => long.TryParse(G(m, k), out var v)?v:null;static bool Bool(Dictionary<string, string>m, string k) => bool.TryParse(G(m, k), out var v) && v;static DateTime? Dt(Dictionary<string, string>m, string k) => DateTime.TryParse(G(m, k), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal, out var v)?v:null;static T E<T>(Dictionary<string, string>m, string k, T d)where T:struct, Enum => Enum.TryParse<T>(G(m, k), true, out var v)?v:d;
}
