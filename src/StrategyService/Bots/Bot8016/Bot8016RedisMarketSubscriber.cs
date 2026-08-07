using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StrategyService.Bots.Bot8016.Configuration;
using TradingSystem.Contracts.Messaging;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016RedisMarketSubscriber(
    IConnectionMultiplexer redis,
    IOptions<Bot8016Options> options,
    Bot8016MarketState state,
    Bot8016EntryCoordinator entry,
    Bot8016PositionLifecycleService lifecycle,
    ILogger<Bot8016RedisMarketSubscriber> logger)
    : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private readonly Bot8016Options _options = options.Value;
    private readonly SemaphoreSlim _processingGate = new(1, 1);
    private Bot8016Candle? _pendingEntryCandle;
    private long _lastProcessedEntryCloseTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var subscriber = redis.GetSubscriber();
            var subscribedChannels = new List<RedisChannel>();

            try
            {
                await SubscribeAsync(subscriber, subscribedChannels, stoppingToken);

                logger.LogInformation(
                    "BOT8016 Redis market subscriptions established. Channels = {Channels}",
                    string.Join(", ", subscribedChannels.Select(x => x.ToString())));

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException exception)
            {
                logger.LogWarning(
                    exception,
                    "BOT8016 could not establish Redis subscriptions. Retrying in {RetryDelay}.",
                    RetryDelay);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "BOT8016 Redis subscription loop failed. Retrying in {RetryDelay}.",
                    RetryDelay);
            }
            finally
            {
                await UnsubscribeSafelyAsync(subscriber, subscribedChannels);
            }

            try
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("BOT8016 Redis market subscriber stopped.");
    }

    private async Task SubscribeAsync(
        ISubscriber subscriber,
        ICollection<RedisChannel> subscribedChannels,
        CancellationToken ct)
    {
        var indicatorChannel = RedisChannel.Literal(_options.AlligatorChannel);

        await subscriber.SubscribeAsync(indicatorChannel, async (_, message) =>
        {
            if (!message.HasValue || ct.IsCancellationRequested)
                return;

            await ProcessWithGateAsync(
                () => ProcessIndicatorAsync(indicatorChannel, message.ToString(), ct),
                "indicator",
                message.ToString(),
                ct);
        });

        subscribedChannels.Add(indicatorChannel);
        logger.LogInformation("BOT8016 subscribed to indicator channel {Channel}", indicatorChannel);

        var marketIntervals = new[] { _options.EntryTimeframe, _options.ExitTimeframe }
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var interval in marketIntervals)
        {
            ct.ThrowIfCancellationRequested();

            var channel = RedisChannel.Literal(RedisChannels.Kline(interval, _options.Symbol));

            await subscriber.SubscribeAsync(channel, async (_, message) =>
            {
                if (!message.HasValue || ct.IsCancellationRequested)
                    return;

                await ProcessWithGateAsync(
                    () => ProcessCandleAsync(message.ToString(), ct),
                    $"kline:{channel}",
                    message.ToString(),
                    ct);
            });

            subscribedChannels.Add(channel);
            logger.LogInformation("BOT8016 subscribed to {Channel}", channel);
        }
    }

    private async Task ProcessWithGateAsync(
        Func<Task> action,
        string source,
        string payload,
        CancellationToken ct)
    {
        var lockTaken = false;

        try
        {
            await _processingGate.WaitAsync(ct);
            lockTaken = true;
            await action();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "BOT8016 invalid Redis JSON. Source = {Source}, Payload = {Payload}",
                source,
                payload);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "BOT8016 Redis message processing failed. Source = {Source}",
                source);
        }
        finally
        {
            if (lockTaken)
                _processingGate.Release();
        }
    }

    private async Task UnsubscribeSafelyAsync(
        ISubscriber subscriber,
        IReadOnlyCollection<RedisChannel> channels)
    {
        foreach (var channel in channels)
        {
            try
            {
                await subscriber.UnsubscribeAsync(channel);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "BOT8016 could not unsubscribe cleanly from {Channel}",
                    channel);
            }
        }

        if (channels.Count > 0)
            logger.LogInformation("BOT8016 unsubscribed from Redis market channels.");
    }

    private async Task ProcessIndicatorAsync(
        RedisChannel channel,
        string json,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "BOT8016 received indicator message. Channel = {Channel}, Payload = {Payload}",
            channel,
            json);

        var indicator = ParseIndicator(json);

        if (indicator is null)
        {
            logger.LogWarning(
                "BOT8016 ignored indicator because the payload could not be parsed as alligator_ma.");
            return;
        }

        if (!IsExpectedIndicator(indicator))
        {
            logger.LogWarning(
                "BOT8016 ignored unexpected indicator. Symbol = {Symbol}, Timeframe = {Timeframe}, CandleCloseTime = {CandleCloseTime}, PublishedAt = {PublishedAt}",
                indicator.Symbol,
                indicator.Timeframe,
                indicator.CandleCloseTime,
                indicator.PublishedAt);
            return;
        }

        state.UpdateIndicator(indicator);

        await TryProcessPendingEntryAsync(indicator, cancellationToken);
    }

    private async Task ProcessCandleAsync(
        string json,
        CancellationToken cancellationToken)
    {
        var candle = ParseCandle(json);

        if (candle is null)
        {
            logger.LogWarning("BOT8016 could not parse kline payload. Payload = {Payload}", json);
            return;
        }

        if (!candle.IsClosed)
            return;

        if (!IsExpectedCandle(candle))
        {
            logger.LogWarning(
                "BOT8016 ignored unexpected candle. Symbol = {Symbol}, Interval = {Interval}, OpenTime = {OpenTime}, CloseTime = {CloseTime}, Close = {Close}",
                candle.Symbol,
                candle.Interval,
                candle.OpenTime,
                candle.CloseTime,
                candle.Close);
            return;
        }

        state.UpdateCandle(candle);

        if (IsEntryTimeframe(candle))
            await ProcessEntryCandleAsync(candle, cancellationToken);

        if (IsExitTimeframe(candle))
            await ProcessExitCandleAsync(candle, cancellationToken);
    }

    private async Task ProcessEntryCandleAsync(
        Bot8016Candle candle,
        CancellationToken cancellationToken)
    {
        if (candle.CloseTime <= _lastProcessedEntryCloseTime)
            return;

        _pendingEntryCandle = candle;
        await TryProcessPendingEntryAsync(state.GetIndicator(), cancellationToken);
    }

    private async Task TryProcessPendingEntryAsync(
        Bot8016IndicatorSnapshot? indicator,
        CancellationToken cancellationToken)
    {
        var candle = _pendingEntryCandle;
        if (candle is null)
            return;

        if (candle.CloseTime <= _lastProcessedEntryCloseTime)
        {
            _pendingEntryCandle = null;
            return;
        }

        if (indicator is null || indicator.CandleCloseTime < candle.CloseTime)
            return;

        if (indicator.CandleCloseTime > candle.CloseTime)
        {
            _pendingEntryCandle = null;
            return;
        }

        if (!IsFresh(indicator))
        {
            _pendingEntryCandle = null;
            return;
        }

        _lastProcessedEntryCloseTime = candle.CloseTime;
        _pendingEntryCandle = null;

        await entry.ProcessAsync(candle, indicator, cancellationToken);
    }

    private async Task ProcessExitCandleAsync(
        Bot8016Candle candle,
        CancellationToken cancellationToken)
    {
        await lifecycle.TryCreateStop3AfterBreakoutAsync(candle.Close, cancellationToken);

        var indicator = state.GetIndicator();
        if (indicator is null || !IsFresh(indicator))
            return;

        await lifecycle.ExitOnTeethCrossAsync(
            candle.Close,
            indicator.Teeth,
            cancellationToken);
    }

    private bool IsEntryTimeframe(Bot8016Candle candle) =>
        candle.Interval.Equals(_options.EntryTimeframe, StringComparison.OrdinalIgnoreCase);

    private bool IsExitTimeframe(Bot8016Candle candle) =>
        candle.Interval.Equals(_options.ExitTimeframe, StringComparison.OrdinalIgnoreCase);

    private bool IsExpectedCandle(Bot8016Candle candle) =>
        candle.Symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase) &&
        (IsEntryTimeframe(candle) || IsExitTimeframe(candle)) &&
        candle.OpenTime > 0 &&
        candle.CloseTime >= candle.OpenTime &&
        candle.Close > 0;

    private bool IsExpectedIndicator(Bot8016IndicatorSnapshot indicator) =>
        indicator.Symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase) &&
        indicator.Timeframe.Equals(_options.EntryTimeframe, StringComparison.OrdinalIgnoreCase) &&
        indicator.CandleCloseTime > 0 &&
        indicator.PublishedAt > 0;

    private bool IsFresh(Bot8016IndicatorSnapshot indicator)
    {
        var age = DateTime.UtcNow - indicator.PublishedAtUtc;
        return age >= TimeSpan.Zero &&
               age <= TimeSpan.FromSeconds(_options.AlligatorMaxAgeSeconds);
    }

    private static Bot8016Candle? ParseCandle(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new Bot8016Candle(
            GetString(root, "symbol") ?? GetString(root, "s") ?? string.Empty,
            GetString(root, "interval") ?? GetString(root, "timeframe") ?? GetString(root, "i") ?? string.Empty,
            GetLong(root, "time", "candle_open_time", "openTime", "open_time", "t"),
            GetLong(root, "close_time", "candle_close_time", "closeTime", "T"),
            GetDecimal(root, "open", "o"),
            GetDecimal(root, "high", "h"),
            GetDecimal(root, "low", "l"),
            GetDecimal(root, "close", "c"),
            GetBool(root, "isClosed", "is_closed", "x", true));
    }

    private static Bot8016IndicatorSnapshot? ParseIndicator(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!string.Equals(
                GetString(root, "type"),
                "alligator_ma",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!root.TryGetProperty("indicators", out var indicators))
            return null;

        return new Bot8016IndicatorSnapshot(
            GetString(root, "symbol") ?? string.Empty,
            GetString(root, "timeframe") ?? string.Empty,
            GetLong(root, "candle_close_time", "close_time"),
            GetLong(root, "published_at", "publishedAt"),
            GetNestedDecimal(indicators, "alligator_jaw"),
            GetNestedDecimal(indicators, "alligator_teeth"),
            GetNestedDecimal(indicators, "alligator_lips"),
            GetNestedDecimal(indicators, "sma200"));
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static long GetLong(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
                continue;

            if (value.TryGetInt64(out var number))
                return number;

            if (long.TryParse(
                    value.ToString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out number))
            {
                return number;
            }
        }

        return 0;
    }

    private static decimal GetDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
                continue;

            if (decimal.TryParse(
                    value.ToString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var number))
            {
                return number;
            }
        }

        return 0;
    }

    private static decimal GetNestedDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var node)
            ? GetDecimal(node, "value")
            : 0;

    private static bool GetBool(
        JsonElement element,
        string firstName,
        string secondName,
        string thirdName,
        bool fallback)
    {
        foreach (var name in new[] { firstName, secondName, thirdName })
        {
            if (!element.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return value.GetBoolean();
        }

        return fallback;
    }

    public override void Dispose()
    {
        _processingGate.Dispose();
        base.Dispose();
    }
}
