using System.Globalization;
using StackExchange.Redis;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;
using TradingSystem.Redis.Constants;

namespace TradingSystem.Redis.Positions;

public sealed class RedisPositionStore(
	IConnectionMultiplexer redis,
	RedisKeyFactory keys)
	: IPositionStore
{
	private readonly IDatabase _database = redis.GetDatabase();

	public async Task SaveAsync(BotPosition position, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var transaction = _database.CreateTransaction();
		_ = transaction.HashSetAsync(keys.Position(position.BotName, position.ShortId), ToEntries(position));
		_ = transaction.SetAddAsync(keys.PositionIndex(position.BotName), position.ShortId);

		if (!await transaction.ExecuteAsync().WaitAsync(ct))
			throw new InvalidOperationException($"Could not save p '{position.ShortId}' for bot '{position.BotName}'.");
	}

	public async Task<BotPosition?> GetAsync(string bot, string id, CancellationToken ct)
	{
		var entries = await _database.HashGetAllAsync(keys.Position(bot, id)).WaitAsync(ct);

		return entries.Length == 0 ? null : FromEntries(entries);
	}

	public async Task<IReadOnlyCollection<BotPosition>> GetAllAsync(string bot, CancellationToken ct)
	{
		var ids = await _database.SetMembersAsync(keys.PositionIndex(bot)).WaitAsync(ct);

		var loadTasks = ids.Select(async id => new
		{
			Id = id,
			Entries = await _database.HashGetAllAsync(keys.Position(bot, id.ToString())).WaitAsync(ct)
		}).ToArray();

		var loaded = await Task.WhenAll(loadTasks).WaitAsync(ct);
		
		var missingIds = loaded.Where(x => x.Entries.Length == 0).Select(x => x.Id).ToArray();
		if (missingIds.Length > 0)
			await _database.SetRemoveAsync(keys.PositionIndex(bot), missingIds).WaitAsync(ct);

		return loaded.Where(x => x.Entries.Length > 0)
			.Select(x => FromEntries(x.Entries))
			.ToArray();
	}

	private static HashEntry[] ToEntries(BotPosition p) =>
	[
		new("short_id", p.ShortId),
		new("bot_name", p.BotName),
		new("symbol", p.Symbol),
		new("side", p.Side.ToString()),
		new("mode", p.Mode.ToString()),
		new("quantity", Decimal(p.Quantity)),
		new("remaining_quantity", Decimal(p.RemainingQuantity)),
		new("entry_price", Decimal(p.EntryPrice)),
		new("parent_client_id", String(p.ParentClientId)),
		new("parent_order_id", String(p.ParentOrderId)),
		new("tp_client_id", String(p.TpClientId)),
		new("tp_order_id", String(p.TpOrderId)),
		new("tp_price", Decimal(p.TpPrice)),
		new("tp_status", String(p.TpStatus)),
		new("tp_executed", Boolean(p.TpExecuted)),
		new("sl_client_id", String(p.SlClientId)),
		new("sl_order_id", String(p.SlOrderId)),
		new("sl_price", Decimal(p.SlPrice)),
		new("sl_status", String(p.SlStatus)),
		new("sl_executed", Boolean(p.SlExecuted)),
		new("stop3_client_id", String(p.Stop3ClientId)),
		new("stop3_order_id", String(p.Stop3OrderId)),
		new("stop3_current", Decimal(p.Stop3Current)),
		new("stop3_initial", Decimal(p.Stop3Initial)),
		new("stop3_previous", Decimal(p.Stop3Previous)),
		new("stop3_new_pending", Decimal(p.Stop3NewPending)),
		new("stop3_status", String(p.Stop3Status)),
		new("stop3_created", Boolean(p.Stop3Created)),
		new("stop3_pending", Boolean(p.Stop3Pending)),
		new("trail_count", p.TrailCount),
		new("trailing_in_progress", Boolean(p.TrailingInProgress)),
		new("close_client_id", String(p.CloseClientId)),
		new("close_order_id", String(p.CloseOrderId)),
		new("close_status", String(p.CloseStatus)),
		new("protective_active", Boolean(p.ProtectiveActive)),
		new("manual_position", Boolean(p.ManualPosition)),
		new("status", p.Status.ToString()),
		new("source", String(p.Source)),
		new("created_at", Time(p.CreatedAtUtc)),
		new("updated_at", Time(p.UpdatedAtUtc)),
		new("parent_filled_at", Time(p.ParentFilledAtUtc)),
		new("tp_filled_at", Time(p.TpFilledAtUtc)),
		new("sl_triggered_at", Time(p.SlTriggeredAtUtc)),
		new("stop3_triggered_at", Time(p.Stop3TriggeredAtUtc)),
		new("closed_at", Time(p.ClosedAtUtc)),
		new("signal_candle_high", Decimal(p.SignalCandleHigh)),
		new("signal_candle_low", Decimal(p.SignalCandleLow)),
		new("signal_candle_close_time", p.SignalCandleCloseTime?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
		new("high_reached", Boolean(p.HighReached))
	];

	private static BotPosition FromEntries(HashEntry[] entries)
	{
		var values = entries.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
		var status = EnumValue(values, "status", PositionStatus.New);

		return new BotPosition
		{
			ShortId = Get(values, "short_id"),
			BotName = Get(values, "bot_name"),
			Symbol = Get(values, "symbol"),
			Side = EnumValue(values, "side", PositionSide.Long),
			Mode = EnumValue(values, "mode", PositionMode.TpOnly),
			Quantity = DecimalValue(values, "quantity") ?? 0,
			RemainingQuantity = DecimalValue(values, "remaining_quantity") ?? 0,
			EntryPrice = DecimalValue(values, "entry_price"),
			ParentClientId = Nullable(values, "parent_client_id"),
			ParentOrderId = Nullable(values, "parent_order_id"),
			TpClientId = Nullable(values, "tp_client_id"),
			TpOrderId = Nullable(values, "tp_order_id"),
			TpPrice = DecimalValue(values, "tp_price"),
			TpStatus = Nullable(values, "tp_status"),
			TpExecuted = Bool(values, "tp_executed"),
			SlClientId = Nullable(values, "sl_client_id"),
			SlOrderId = Nullable(values, "sl_order_id"),
			SlPrice = DecimalValue(values, "sl_price"),
			SlStatus = Nullable(values, "sl_status"),
			SlExecuted = Bool(values, "sl_executed"),
			Stop3ClientId = Nullable(values, "stop3_client_id"),
			Stop3OrderId = Nullable(values, "stop3_order_id"),
			Stop3Current = DecimalValue(values, "stop3_current"),
			Stop3Initial = DecimalValue(values, "stop3_initial"),
			Stop3Previous = DecimalValue(values, "stop3_previous"),
			Stop3NewPending = DecimalValue(values, "stop3_new_pending"),
			Stop3Status = Nullable(values, "stop3_status"),
			Stop3Created = Bool(values, "stop3_created"),
			Stop3Pending = Bool(values, "stop3_pending"),
			TrailCount = Int(values, "trail_count"),
			TrailingInProgress = Bool(values, "trailing_in_progress"),
			CloseClientId = Nullable(values, "close_client_id"),
			CloseOrderId = Nullable(values, "close_order_id"),
			CloseStatus = Nullable(values, "close_status"),
			ProtectiveActive = Bool(values, "protective_active"),
			ManualPosition = Bool(values, "manual_position"),
			Status = status,
			Closed = status == PositionStatus.Closed || Date(values, "closed_at").HasValue,
			Source = Nullable(values, "source"),
			CreatedAtUtc = Date(values, "created_at") ?? DateTime.UtcNow,
			UpdatedAtUtc = Date(values, "updated_at"),
			ParentFilledAtUtc = Date(values, "parent_filled_at"),
			TpFilledAtUtc = Date(values, "tp_filled_at"),
			SlTriggeredAtUtc = Date(values, "sl_triggered_at"),
			Stop3TriggeredAtUtc = Date(values, "stop3_triggered_at"),
			ClosedAtUtc = Date(values, "closed_at"),
			SignalCandleHigh = DecimalValue(values, "signal_candle_high"),
			SignalCandleLow = DecimalValue(values, "signal_candle_low"),
			SignalCandleCloseTime = Long(values, "signal_candle_close_time"),
			HighReached = Bool(values, "high_reached")
		};
	}

	private static string String(string? value) 
		=> value ?? string.Empty;
	
	private static string Decimal(decimal? value) 
		=> value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
	
	private static string Decimal(decimal value) 
		=> value.ToString(CultureInfo.InvariantCulture);
	
	private static string Boolean(bool value)
		=> value ? "true" : "false";
	
	private static string Time(DateTime? value) 
		=> value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
	
	private static string Time(DateTime value) 
		=> value.ToString("O", CultureInfo.InvariantCulture);

	private static string Get(Dictionary<string, string> values, string key, string fallback = "")
		=> values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

	private static string? Nullable(Dictionary<string, string> values, string key)
		=> values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

	private static decimal? DecimalValue(Dictionary<string, string> values, string key)
		=> decimal.TryParse(Get(values, key), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;

	private static int Int(Dictionary<string, string> values, string key)
		=> int.TryParse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

	private static long? Long(Dictionary<string, string> values, string key)
		=> long.TryParse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

	private static bool Bool(Dictionary<string, string> values, string key)
		=> bool.TryParse(Get(values, key), out var value) && value;

	private static DateTime? Date(Dictionary<string, string> values, string key)
		=> DateTime.TryParse(Get(values, key),CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value) ? value: null;

	private static T EnumValue<T>(Dictionary<string, string> values, string key, T fallback) where T : struct, Enum
		=> Enum.TryParse<T>(Get(values, key), true, out var value) ? value : fallback;
}
