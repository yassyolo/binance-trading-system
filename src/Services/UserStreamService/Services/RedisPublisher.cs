using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;

namespace UserStreamService.Services;

public sealed class RedisPublisher(
    IConnectionMultiplexer redis,
    IConfiguration configuration,
    ILogger<RedisPublisher> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task PublishRawAsync(object payload)
    {
        var publishRaw = configuration.GetValue<bool>("UserStream:PublishRaw", true);

        if (!publishRaw)
            return;

        await PublishAsync(RedisChannels.UserStreamRaw, payload);
    }

    public Task PublishOrderAsync(object payload)
        => PublishAsync(RedisChannels.UserStreamOrder, payload);

    public Task PublishAccountAsync(object payload)
        => PublishAsync(RedisChannels.UserStreamAccount, payload);

    public Task PublishHealingAsync(object payload)
        => PublishAsync(RedisChannels.Healing, payload);

    private async Task PublishAsync(string channel, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), json);

        logger.LogInformation("Redis publish. Channel={Channel}", channel);
    }
}