using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;
using TradingSystem.Infrastructure.Serialization;
using UserStreamService.Configuration;

namespace UserStreamService.Services;

public sealed class RedisPublisher(
    IConnectionMultiplexer redis,
    IOptions<UserStreamOptions> options,
    ILogger<RedisPublisher> logger)
{
    private readonly ISubscriber _subscriber = redis.GetSubscriber();
    private readonly UserStreamOptions _options = options.Value;

    public Task PublishRawAsync(object payload)
        => _options.PublishRaw ? PublishAsync(RedisChannels.UserStreamRaw, payload) : Task.CompletedTask;

    public Task PublishOrderAsync(object payload)
        => PublishAsync(RedisChannels.UserStreamOrder, payload);

    public Task PublishAccountAsync(object payload)
        => PublishAsync(RedisChannels.UserStreamAccount, payload);

    public Task PublishHealingAsync(object payload)
        => PublishAsync(RedisChannels.Healing, payload);

    private async Task PublishAsync(string channel, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonDefaults.SnakeCase);
        await _subscriber.PublishAsync(RedisChannel.Literal(channel), json);
        logger.LogDebug("Redis message published. Channel={Channel}", channel);
    }
}
