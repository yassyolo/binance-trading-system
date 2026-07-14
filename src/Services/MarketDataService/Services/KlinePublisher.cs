using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Contracts.Klines;
using TradingSystem.Contracts.Redis;
using TradingSystem.Infrastructure.Serialization;

namespace MarketDataService.Services;

public sealed class KlinePublisher(
    IConnectionMultiplexer redis,
    ILogger<KlinePublisher> logger)
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly ISubscriber _subscriber = redis.GetSubscriber();

    public async Task PublishClosedKlineAsync(
        JsonElement kline,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetRequiredString(kline, "s", out var symbol) ||
            !TryGetRequiredString(kline, "i", out var interval) ||
            !TryGetInt64(kline, "t", out var openTime) ||
            !TryGetInt64(kline, "T", out var closeTime))
        {
            logger.LogWarning("Invalid Binance kline payload. Payload={Payload}", kline.GetRawText());
            return;
        }

        symbol = symbol.ToUpperInvariant();
        interval = interval.ToLowerInvariant();

        var message = new ClosedKlineMessage(
            Symbol: symbol,
            Time: openTime,
            Open: GetOptionalString(kline, "o"),
            High: GetOptionalString(kline, "h"),
            Low: GetOptionalString(kline, "l"),
            Close: GetOptionalString(kline, "c"),
            Volume: GetOptionalString(kline, "v"),
            CloseTime: closeTime,
            Interval: interval);

        var key = RedisKeys.Kline(symbol, interval);
        var channel = RedisChannels.Kline(interval, symbol);
        var rawPayload = kline.GetRawText();
        var cleanPayload = JsonSerializer.Serialize(message, JsonDefaults.SnakeCase);

        await _database.StringSetAsync(key, rawPayload);
        await _subscriber.PublishAsync(RedisChannel.Literal(channel), cleanPayload);

        logger.LogInformation(
            "Closed kline published. Symbol={Symbol}, Interval={Interval}, Close={Close}, Key={Key}, Channel={Channel}",
            symbol,
            interval,
            message.Close,
            key,
            channel);
    }

    private static bool TryGetRequiredString(JsonElement element, string name, out string value)
    {
        value = string.Empty;

        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
            return false;

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string? GetOptionalString(JsonElement element, string name)
        => element.TryGetProperty(name, out var property)
            ? property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString()
            : null;

    private static bool TryGetInt64(JsonElement element, string name, out long value)
    {
        value = default;
        return element.TryGetProperty(name, out var property) && property.TryGetInt64(out value);
    }
}
