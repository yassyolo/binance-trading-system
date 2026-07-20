namespace TradingSystem.Application.Events;
public interface IEventDeduplicationStore
{
    Task<bool> TryBeginAsync(string eventKey,  TimeSpan ttl,  CancellationToken cancellationToken);
}
