namespace TradingSystem.Application.Engine;

public interface ISignalIdempotencyStore
{
    Task<bool> TryStartAsync(string signalId,  TimeSpan ttl,  CancellationToken cancellationToken);
    Task MarkCompletedAsync(string signalId,  TimeSpan ttl,  CancellationToken cancellationToken);
    Task ReleaseAsync(string signalId,  CancellationToken cancellationToken);
}
