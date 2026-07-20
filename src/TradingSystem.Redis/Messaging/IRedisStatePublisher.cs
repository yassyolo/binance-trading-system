namespace TradingSystem.Redis.Messaging;
public interface IRedisStatePublisher
{
    Task SetAndPublishAsync(string key,  string channel,  object payload,  CancellationToken cancellationToken);
}
