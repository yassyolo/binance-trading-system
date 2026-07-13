using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StrategyService.Configuration;

namespace StrategyService.Market;

public sealed class Bot8011KlineSubscriber(
    IConnectionMultiplexer redis,
    IOptions<Bot8011Options> options,
    Bot8011TrailingPriceCache cache,
    ILogger<Bot8011KlineSubscriber> logger)
    : BackgroundService
{
    private readonly Bot8011Options _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel =
            $"futures_kline_channel:{_options.TrailingInterval}:{_options.Symbol}";

        var subscriber = redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(channel),
            (_, message) =>
            {
                try
                {
                    using var document = JsonDocument.Parse(message.ToString());
                    var root = document.RootElement;

                    var close = GetDecimal(root, "close", "c");
                    if (close > 0)
                        cache.Set(close);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "BOT8011 invalid kline payload.");
                }

                return;
            });

        logger.LogInformation("BOT8011 subscribed to {Channel}", channel);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static decimal GetDecimal(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number
                && value.TryGetDecimal(out var number))
                return number;

            if (decimal.TryParse(
                    value.GetString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out number))
                return number;
        }

        return 0;
    }
}
