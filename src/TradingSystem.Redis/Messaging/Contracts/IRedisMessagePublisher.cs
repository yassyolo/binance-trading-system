namespace TradingSystem.Redis.Messaging.Contracts;

public interface IRedisMessagePublisher
{
    Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default);
}
