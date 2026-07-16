using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Configuration;
using TradingSystem.Signals.Models;

namespace TradingSystem.Signals.Services;

public sealed class RedisSignalPublisher(
    IConnectionMultiplexer redis,
    IOptions<SignalGenerationOptions> options) : ISignalPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _channel = options.Value.RedisChannel;

    public Task PublishAsync(GeneratedTradingSignal signal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(signal, JsonOptions);
        return redis.GetSubscriber().PublishAsync(RedisChannel.Literal(_channel), payload);
    }
}
