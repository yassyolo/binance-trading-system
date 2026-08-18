using System.Globalization;
using System.Text.Json;
using MarketDataService.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Klines;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Infrastructure.Serialization;

namespace MarketDataService.Services;

public sealed class KlinePublisher(
    IConnectionMultiplexer redis,
    IOptions<MarketDataOptions> options,
    ILogger<KlinePublisher> logger)
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly ISubscriber _subscriber = redis.GetSubscriber();
    private readonly TimeSpan _latestKlineTtl = TimeSpan.FromSeconds(options.Value.LatestKlineTtlSeconds);

    public async Task PublishAsync(JsonElement kline, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!TryReadString(kline, "s", out var symbol) ||
            !TryReadString(kline, "i", out var interval) ||
            !TryReadLong(kline, "t", out var openTime) ||
            !TryReadLong(kline, "T", out var closeTime) ||
            closeTime < openTime ||
            !TryReadDecimal(kline, "o", out var open) ||
            !TryReadDecimal(kline, "h", out var high) ||
            !TryReadDecimal(kline, "l", out var low) ||
            !TryReadDecimal(kline, "c", out var close) ||
            !TryReadDecimal(kline, "v", out var volume) ||
            high < low ||
            open < low || open > high ||
            close < low || close > high ||
            volume < 0)
        {
            logger.LogWarning("Rejected malformed closed Binance kline payload.");
           
            return;
        }

        symbol = symbol.Trim().ToUpperInvariant();
        interval = interval.Trim().ToLowerInvariant();

        var message = new ClosedKlineMessage(
            symbol,
            openTime,
            Format(open),
            Format(high),
            Format(low),
            Format(close),
            Format(volume),
            closeTime,
            interval);

        var json = JsonSerializer.Serialize(message, JsonDefaults.Messaging);
       
        var key = $"kline:{symbol.ToLowerInvariant()}:{interval}";
        
        var channel = RedisChannels.Kline(interval, symbol);

        await _database.StringSetAsync(key, json, _latestKlineTtl);
        await _subscriber.PublishAsync(RedisChannel.Literal(channel), json);

        logger.LogInformation("Closed kline published. Symbol = {Symbol}, Interval = {Interval}, CloseTime = {CloseTime}", symbol, interval, closeTime);
    }

    private static bool TryReadString(JsonElement element, string name, out string value)
    {
        value = element.TryGetProperty(name, out var property)
            ? property.GetString() ?? string.Empty : string.Empty;
       
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadLong(JsonElement element, string name, out long value)
    {
        value = 0;
        return element.TryGetProperty(name, out var property) && property.TryGetInt64(out value);
    }

    private static bool TryReadDecimal(JsonElement element, string name, out decimal value)
    {
        value = 0;
        if (!element.TryGetProperty(name, out var property))
            return false;

        return decimal.TryParse(property.ToString(), NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out value);
    }

    private static string Format(decimal value) 
        => value.ToString(CultureInfo.InvariantCulture);
}
