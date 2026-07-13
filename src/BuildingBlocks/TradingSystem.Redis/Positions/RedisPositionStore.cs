using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Globalization;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;
using TradingSystem.Redis.Configuration;

namespace TradingSystem.Redis.Positions;

public sealed class RedisPositionStore : IPositionStore
{
    private readonly IDatabase _database;
    private readonly IServer _server;
    private readonly RedisPositionStoreOptions options;

    public RedisPositionStore(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisPositionStoreOptions> _options)
    {
        _database = connectionMultiplexer.GetDatabase();
        _server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().First());
        options = _options.Value;
    }

    public async Task SaveAsync(BotPosition position, CancellationToken cancellationToken)
    {
        var key = GetPositionKey(position.BotName, position.ShortId);

        position.UpdatedAtUtc = DateTime.UtcNow;

        await _database.HashSetAsync(key, ToHashEntries(position));
    }

    public async Task<BotPosition?> GetAsync(
        string botName,
        string shortId,
        CancellationToken cancellationToken)
    {
        var key = GetPositionKey(botName, shortId);

        var entries = await _database.HashGetAllAsync(key);

        if (entries.Length == 0)
            return null;

        return FromHashEntries(entries);
    }

    public async Task<IReadOnlyCollection<BotPosition>> GetAllAsync(
    string botName,
    CancellationToken cancellationToken)
    {
        RedisValue pattern = GetPositionKey(botName, "*").ToString();

        var result = new List<BotPosition>();

        foreach (var key in _server.Keys(pattern: pattern))
        {
            if (key.ToString().EndsWith(":lock", StringComparison.OrdinalIgnoreCase))
                continue;

            var entries = await _database.HashGetAllAsync(key);

            if (entries.Length == 0)
                continue;

            var position = FromHashEntries(entries);

            result.Add(position);
        }

        return result;
    }

    public Task DeleteAsync(
        string botName,
        string shortId,
        CancellationToken cancellationToken)
    {
        var key = GetPositionKey(botName, shortId);

        return _database.KeyDeleteAsync(key);
    }

    private RedisKey GetPositionKey(string botName, string shortId)
    {
        var key = $"{botName}:position:{shortId}";

        return string.IsNullOrWhiteSpace(options.Prefix)
            ? key
            : $"{options.Prefix}:{key}";
    }

    private static HashEntry[] ToHashEntries(BotPosition position)
    {
        return
        [
            new("short_id", position.ShortId),
            new("bot_name", position.BotName),
            new("symbol", position.Symbol),
            new("side", position.Side.ToString().ToUpperInvariant()),
            new("mode", position.Mode.ToString()),

            new("quantity", ToRedis(position.Quantity)),
            new("remaining_quantity", ToRedis(position.RemainingQuantity)),
            new("entry_price", ToRedis(position.EntryPrice)),

            new("parent_client_id", position.ParentClientId ?? string.Empty),
            new("parent_order_id", position.ParentOrderId ?? string.Empty),

            new("tp_client_id", position.TpClientId ?? string.Empty),
            new("tp_order_id", position.TpOrderId ?? string.Empty),
            new("tp_price", ToRedis(position.TpPrice)),
            new("tp_status", position.TpStatus ?? string.Empty),
            new("tp_executed", ToRedis(position.TpExecuted)),

            new("sl_client_id", position.SlClientId ?? string.Empty),
            new("sl_order_id", position.SlOrderId ?? string.Empty),
            new("sl_price", ToRedis(position.SlPrice)),
            new("sl_status", position.SlStatus ?? string.Empty),
            new("sl_executed", ToRedis(position.SlExecuted)),

            new("stop3_client_id", position.Stop3ClientId ?? string.Empty),
            new("stop3_order_id", position.Stop3OrderId ?? string.Empty),
            new("stop3_current", ToRedis(position.Stop3Current)),
            new("stop3_initial", ToRedis(position.Stop3Initial)),
            new("stop3_previous", ToRedis(position.Stop3Previous)),
            new("stop3_new_pending", ToRedis(position.Stop3NewPending)),
            new("stop3_status", position.Stop3Status ?? string.Empty),
            new("stop3_created", ToRedis(position.Stop3Created)),
            new("stop3_pending", ToRedis(position.Stop3Pending)),

            new("trail_count", position.TrailCount),
            new("trailing_in_progress", ToRedis(position.TrailingInProgress)),

            new("close_client_id", position.CloseClientId ?? string.Empty),
            new("close_order_id", position.CloseOrderId ?? string.Empty),
            new("close_status", position.CloseStatus ?? string.Empty),

            new("protective_active", ToRedis(position.ProtectiveActive)),
            new("manual_position", ToRedis(position.ManualPosition)),
            new("closed", ToRedis(position.Closed)),

            new("status", position.Status.ToString()),
            new("source", position.Source ?? string.Empty),

            new("created_at", ToRedis(position.CreatedAtUtc)),
            new("updated_at", ToRedis(position.UpdatedAtUtc)),

            new("parent_filled_at", ToRedis(position.ParentFilledAtUtc)),
            new("tp_filled_at", ToRedis(position.TpFilledAtUtc)),
            new("sl_triggered_at", ToRedis(position.SlTriggeredAtUtc)),
            new("stop3_triggered_at", ToRedis(position.Stop3TriggeredAtUtc)),
            new("closed_at", ToRedis(position.ClosedAtUtc))
        ];
    }

    private static BotPosition FromHashEntries(HashEntry[] entries)
    {
        var map = entries.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

        return new BotPosition
        {
            ShortId = GetString(map, "short_id"),
            BotName = GetString(map, "bot_name"),
            Symbol = GetString(map, "symbol"),

            Side = ParseSide(GetString(map, "side")),
            Mode = ParseMode(GetString(map, "mode")),

            Quantity = GetDecimal(map, "quantity") ?? 0,
            RemainingQuantity = GetDecimal(map, "remaining_quantity") ?? 0,
            EntryPrice = GetDecimal(map, "entry_price"),

            ParentClientId = GetNullableString(map, "parent_client_id"),
            ParentOrderId = GetNullableString(map, "parent_order_id"),

            TpClientId = GetNullableString(map, "tp_client_id"),
            TpOrderId = GetNullableString(map, "tp_order_id"),
            TpPrice = GetDecimal(map, "tp_price"),
            TpStatus = GetNullableString(map, "tp_status"),
            TpExecuted = GetBool(map, "tp_executed"),

            SlClientId = GetNullableString(map, "sl_client_id"),
            SlOrderId = GetNullableString(map, "sl_order_id"),
            SlPrice = GetDecimal(map, "sl_price"),
            SlStatus = GetNullableString(map, "sl_status"),
            SlExecuted = GetBool(map, "sl_executed"),

            Stop3ClientId = GetNullableString(map, "stop3_client_id"),
            Stop3OrderId = GetNullableString(map, "stop3_order_id"),
            Stop3Current = GetDecimal(map, "stop3_current"),
            Stop3Initial = GetDecimal(map, "stop3_initial"),
            Stop3Previous = GetDecimal(map, "stop3_previous"),
            Stop3NewPending = GetDecimal(map, "stop3_new_pending"),
            Stop3Status = GetNullableString(map, "stop3_status"),
            Stop3Created = GetBool(map, "stop3_created"),
            Stop3Pending = GetBool(map, "stop3_pending"),

            TrailCount = GetInt(map, "trail_count"),
            TrailingInProgress = GetBool(map, "trailing_in_progress"),

            CloseClientId = GetNullableString(map, "close_client_id"),
            CloseOrderId = GetNullableString(map, "close_order_id"),
            CloseStatus = GetNullableString(map, "close_status"),

            ProtectiveActive = GetBool(map, "protective_active"),
            ManualPosition = GetBool(map, "manual_position"),
            Closed = GetBool(map, "closed"),

            Status = ParseStatus(GetString(map, "status", "New")),
            Source = GetNullableString(map, "source"),

            CreatedAtUtc = GetDateTime(map, "created_at") ?? DateTime.UtcNow,
            UpdatedAtUtc = GetDateTime(map, "updated_at"),

            ParentFilledAtUtc = GetDateTime(map, "parent_filled_at"),
            TpFilledAtUtc = GetDateTime(map, "tp_filled_at"),
            SlTriggeredAtUtc = GetDateTime(map, "sl_triggered_at"),
            Stop3TriggeredAtUtc = GetDateTime(map, "stop3_triggered_at"),
            ClosedAtUtc = GetDateTime(map, "closed_at")
        };
    }

    private static PositionStatus ParseStatus(string value)
    {
        return Enum.TryParse<PositionStatus>(
            value,
            ignoreCase: true,
            out var result)
            ? result
            : PositionStatus.New;
    }

    private static string GetString(
        IReadOnlyDictionary<string, string> map,
        string key,
        string defaultValue = "")
        => map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;

    private static string? GetNullableString(IReadOnlyDictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var value))
            return null;

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal? GetDecimal(IReadOnlyDictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static int GetInt(IReadOnlyDictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return 0;

        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var value))
            return false;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime? GetDateTime(IReadOnlyDictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var result)
            ? result
            : null;
    }

    private static PositionSide ParseSide(string value)
    {
        return value.Equals("SHORT", StringComparison.OrdinalIgnoreCase)
            ? PositionSide.Short
            : PositionSide.Long;
    }

    private static PositionMode ParseMode(string value)
    {
        return value.Equals("TpOnly", StringComparison.OrdinalIgnoreCase)
            ? PositionMode.TpOnly
            : PositionMode.Stop3;
    }

    private static string ToRedis(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string ToRedis(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string ToRedis(bool value)
        => value ? "True" : "False";

    private static string ToRedis(DateTime? value)
        => value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string ToRedis(DateTime value)
        => value.ToString("O", CultureInfo.InvariantCulture);
}