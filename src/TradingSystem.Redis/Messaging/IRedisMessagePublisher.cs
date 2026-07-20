namespace TradingSystem.Redis.Messaging;

public interface IRedisMessagePublisher
{
    Task PublishAsync<T>(string channel,  T message,  CancellationToken cancellationToken  =  default);
}
