namespace TradingSystem.Application.Engine;

public interface ISignalIdempotencyStore
{
    Task<bool> TryStartAsync(string signalId,  TimeSpan ttl,  CancellationToken ct);
    Task MarkCompletedAsync(string signalId,  TimeSpan ttl,  CancellationToken ct);
    Task ReleaseAsync(string signalId,  CancellationToken ct);
}
