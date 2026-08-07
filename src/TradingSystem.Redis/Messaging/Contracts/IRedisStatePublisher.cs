namespace TradingSystem.Redis.Messaging.Contracts;

public interface IRedisStatePublisher
{
    Task SetAndPublishAsync(string key, string channel, object payload, CancellationToken ct);
}
