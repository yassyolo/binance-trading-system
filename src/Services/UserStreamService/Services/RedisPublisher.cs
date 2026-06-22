using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;

namespace UserStreamService.Services;

public sealed class RedisPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedisPublisher> _logger;

    public RedisPublisher(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisPublisher> logger)
    {
        _redis = redis;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishRawAsync(object payload)
    {
        var publishRaw = _configuration.GetValue<bool>("UserStream:PublishRaw", true);

        if (!publishRaw)
            return;

        await PublishAsync(RedisNames.UserStreamRaw, payload);
    }

    public Task PublishOrderAsync(object payload)
        => PublishAsync(RedisNames.UserStreamOrder, payload);

    public Task PublishAccountAsync(object payload)
        => PublishAsync(RedisNames.UserStreamAccount, payload);

    public Task PublishHealingAsync(object payload)
        => PublishAsync(RedisNames.HealingChannel, payload);

    private async Task PublishAsync(string channel, object payload)
    {
        var json = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

        await _redis
            .GetSubscriber()
            .PublishAsync(RedisChannel.Literal(channel), json);

        _logger.LogInformation("Redis publish. Channel={Channel}", channel);
    }
}