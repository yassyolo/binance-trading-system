using System.Globalization;
using System.Text.Json;
using AlligatorIndicatorService.Configuration;
using AlligatorIndicatorService.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AlligatorIndicatorService.Services;

public sealed class AlligatorRedisSubscriber(
    IConnectionMultiplexer redis,
    IOptions<AlligatorOptions> options,
    AlligatorMaEngine engine,
    ILogger<AlligatorRedisSubscriber> logger)
    : BackgroundService
{
    private readonly AlligatorOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();

        foreach (var symbol in _options.Symbols)
        {
            foreach (var interval in _options.Intervals)
            {
                var channel = $"futures_kline_channel:{interval}:{symbol}";

                await subscriber.SubscribeAsync(
                    RedisChannel.Literal(channel),
                    async (_, message) =>
                    {
                        if (!message.HasValue)
                            return;

                        try
                        {
                            var candle = Parse(
                                symbol,
                                interval,
                                message.ToString());

                            var payload = engine.Process(
                                symbol,
                                interval,
                                candle);

                            if (payload is null)
                                return;

                            var json = JsonSerializer.Serialize(payload);

                            await subscriber.PublishAsync(
                                RedisChannel.Literal(_options.RedisOutputChannel),
                                json);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(
                                ex,
                                "Alligator kline processing failed. Symbol={Symbol}, Interval={Interval}",
                                symbol,
                                interval);
                        }
                    });

                logger.LogInformation(
                    "Alligator subscribed to {Channel}",
                    channel);
            }
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static Candle Parse(
        string symbol,
        string interval,
        string json)
    {
        using var doc = JsonDocument.Parse(json);
        var x = doc.RootElement;

        return new Candle
        {
            Symbol = symbol.ToUpperInvariant(),
            Interval = interval.ToLowerInvariant(),
            OpenTime = GetLong(x, "open_time", "t"),
            CloseTime = GetLong(x, "close_time", "T"),
            Open = GetDecimal(x, "open", "o"),
            High = GetDecimal(x, "high", "h"),
            Low = GetDecimal(x, "low", "l"),
            Close = GetDecimal(x, "close", "c"),
            IsClosed = GetBool(x, "is_closed", "x", true)
        };
    }

    private static long GetLong(JsonElement e, params string[] names)
    {
        foreach (var name in names)
        {
            if (!e.TryGetProperty(name, out var v))
                continue;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n))
                return n;
            if (long.TryParse(v.GetString(), out n))
                return n;
        }
        return 0;
    }

    private static decimal GetDecimal(JsonElement e, params string[] names)
    {
        foreach (var name in names)
        {
            if (!e.TryGetProperty(name, out var v))
                continue;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var n))
                return n;
            if (decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out n))
                return n;
        }
        return 0;
    }

    private static bool GetBool(
        JsonElement e,
        string a,
        string b,
        bool defaultValue)
    {
        foreach (var name in new[] { a, b })
        {
            if (!e.TryGetProperty(name, out var v))
                continue;
            if (v.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return v.GetBoolean();
        }
        return defaultValue;
    }
}
