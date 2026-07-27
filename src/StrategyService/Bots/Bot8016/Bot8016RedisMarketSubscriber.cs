using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Messaging;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016RedisMarketSubscriber(
    IConnectionMultiplexer redis,
    IOptions<Bot8016Options> options,
    Bot8016MarketState state,
    Bot8016EntryCoordinator entry,
    Bot8016PositionLifecycleService lifecycle,
    ILogger<Bot8016RedisMarketSubscriber> logger) : BackgroundService
{
    private readonly Bot8016Options _options = options.Value;
    private readonly SemaphoreSlim _processingGate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();
        var channels = new List<RedisChannel>();

        var indicatorChannel = RedisChannel.Literal(_options.AlligatorChannel);
        channels.Add(indicatorChannel);
        await subscriber.SubscribeAsync(indicatorChannel, async (_, message) =>
        {
            if (!message.HasValue) return;
            try
            {
                var snapshot = ParseIndicator(message.ToString());
                if (snapshot is not null && IsExpectedIndicator(snapshot)) state.UpdateIndicator(snapshot);
            }
            catch (JsonException ex) { logger.LogWarning(ex, "BOT8016 invalid indicator JSON."); }
            catch (Exception ex) { logger.LogWarning(ex, "BOT8016 indicator message failed."); }
            await Task.CompletedTask;
        });

        foreach (var interval in new[] { _options.EntryTimeframe, _options.ExitTimeframe }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var channel = RedisChannel.Literal(RedisChannels.Kline(interval, _options.Symbol));
            channels.Add(channel);
            await subscriber.SubscribeAsync(channel, async (_, message) =>
            {
                if (!message.HasValue) return;
                await _processingGate.WaitAsync(stoppingToken);
                try { await ProcessCandleAsync(message.ToString(), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch (JsonException ex) { logger.LogWarning(ex, "BOT8016 invalid kline JSON."); }
                catch (Exception ex) { logger.LogError(ex, "BOT8016 kline message failed."); }
                finally { _processingGate.Release(); }
            });
            logger.LogInformation("BOT8016 subscribed to {Channel}", channel);
        }

        try { await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            foreach (var channel in channels) await subscriber.UnsubscribeAsync(channel);
        }
    }

    private async Task ProcessCandleAsync(string json, CancellationToken cancellationToken)
    {
        var candle = ParseCandle(json);
        if (candle is null || !candle.IsClosed || !IsExpectedCandle(candle)) return;
        state.UpdateCandle(candle);

        var indicator = state.GetIndicator();
        if (candle.Interval.Equals(_options.EntryTimeframe, StringComparison.OrdinalIgnoreCase))
        {
            if (indicator is null || !IsFreshAndAligned(indicator, candle)) return;
            await entry.ProcessAsync(candle, indicator, cancellationToken);
        }

        if (candle.Interval.Equals(_options.ExitTimeframe, StringComparison.OrdinalIgnoreCase))
        {
            await lifecycle.TryCreateStop3AfterBreakoutAsync(candle.Close, cancellationToken);
            if (indicator is not null && IsFresh(indicator))
                await lifecycle.ExitOnTeethCrossAsync(candle.Close, indicator.Teeth, cancellationToken);
        }
    }

    private bool IsExpectedCandle(Bot8016Candle candle) =>
        candle.Symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase) &&
        (candle.Interval.Equals(_options.EntryTimeframe, StringComparison.OrdinalIgnoreCase) ||
         candle.Interval.Equals(_options.ExitTimeframe, StringComparison.OrdinalIgnoreCase)) &&
        candle.OpenTime > 0 && candle.CloseTime >= candle.OpenTime && candle.Close > 0;

    private bool IsExpectedIndicator(Bot8016IndicatorSnapshot indicator) =>
        indicator.Symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase) &&
        indicator.Timeframe.Equals(_options.EntryTimeframe, StringComparison.OrdinalIgnoreCase) &&
        indicator.CandleCloseTime > 0 && indicator.PublishedAt > 0;

    private bool IsFresh(Bot8016IndicatorSnapshot indicator) =>
        DateTime.UtcNow - indicator.PublishedAtUtc <= TimeSpan.FromSeconds(_options.AlligatorMaxAgeSeconds);

    private bool IsFreshAndAligned(Bot8016IndicatorSnapshot indicator, Bot8016Candle candle) =>
        IsFresh(indicator) && indicator.CandleCloseTime == candle.CloseTime;

    private static Bot8016Candle? ParseCandle(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var x = doc.RootElement;
        return new(GetString(x, "symbol") ?? GetString(x, "s") ?? "", GetString(x, "interval") ?? GetString(x, "i") ?? "",
            GetLong(x, "openTime", "open_time", "t"), GetLong(x, "closeTime", "close_time", "T"),
            GetDecimal(x, "open", "o"), GetDecimal(x, "high", "h"), GetDecimal(x, "low", "l"), GetDecimal(x, "close", "c"),
            GetBool(x, "isClosed", "is_closed", "x", true));
    }

    private static Bot8016IndicatorSnapshot? ParseIndicator(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!string.Equals(GetString(root, "type"), "alligator_ma", StringComparison.OrdinalIgnoreCase)) return null;
        if (!root.TryGetProperty("indicators", out var indicators)) return null;
        return new(GetString(root, "symbol") ?? "", GetString(root, "timeframe") ?? "", GetLong(root, "candle_close_time"), GetLong(root, "published_at"),
            GetNestedDecimal(indicators, "alligator_jaw"), GetNestedDecimal(indicators, "alligator_teeth"), GetNestedDecimal(indicators, "alligator_lips"), GetNestedDecimal(indicators, "sma200"));
    }

    private static string? GetString(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static long GetLong(JsonElement e, params string[] names) { foreach (var name in names) if (e.TryGetProperty(name, out var v) && (v.TryGetInt64(out var n) || long.TryParse(v.ToString(), out n))) return n; return 0; }
    private static decimal GetDecimal(JsonElement e, params string[] names) { foreach (var name in names) if (e.TryGetProperty(name, out var v) && decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n)) return n; return 0; }
    private static decimal GetNestedDecimal(JsonElement e, string name) => e.TryGetProperty(name, out var node) ? GetDecimal(node, "value") : 0;
    private static bool GetBool(JsonElement e, string a, string b, string c, bool fallback) { foreach (var name in new[] { a, b, c }) if (e.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False) return v.GetBoolean(); return fallback; }
}
