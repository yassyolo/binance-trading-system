using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StrategyService.Configuration;
using TradingSystem.Contracts.Redis;

namespace StrategyService.Market;

public sealed class Bot8015KlineSubscriber(
    IConnectionMultiplexer redis,
    IOptions<Bot8015Options> options,
    Bot8015TrailingPriceCache cache,
    ILogger<Bot8015KlineSubscriber> logger)
    : BackgroundService
{
    private readonly Bot8015Options _options = options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var channel =
            $"futures_kline_channel:{_options.KlineInterval}:{_options.Symbol}";

        var subscriber = redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(channel),
            (_, message) =>
            {
                if (!message.HasValue)
                    return;

                try
                {
                    Process(message.ToString());
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "BOT8015 failed to process kline message.");
                }
            });

        logger.LogInformation(
            "BOT8015 subscribed to kline channel {Channel}",
            channel);

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }

    private void Process(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var close = GetDecimal(root, "close");
        if (close <= 0)
            close = GetDecimal(root, "c");

        if (close <= 0)
            return;

        var closeTimeMs = GetLong(root, "close_time");
        if (closeTimeMs <= 0)
            closeTimeMs = GetLong(root, "T");

        var closeAtUtc = closeTimeMs > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(closeTimeMs).UtcDateTime
            : DateTime.UtcNow;

        cache.Update(close, closeAtUtc);
    }

    private static decimal GetDecimal(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return 0;

        if (value.ValueKind == JsonValueKind.Number
            && value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && decimal.TryParse(
                value.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static long GetLong(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return 0;

        if (value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && long.TryParse(
                value.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return parsed;
        }

        return 0;
    }
}
