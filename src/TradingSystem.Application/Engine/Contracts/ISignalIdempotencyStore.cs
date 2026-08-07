namespace TradingSystem.Application.Engine.Contracts;

public interface ISignalIdempotencyStore
{
    Task<bool> TryStartAsync(string signalId,  TimeSpan ttl,  CancellationToken ct);
    
    Task MarkCompletedAsync(string signalId,  TimeSpan ttl,  CancellationToken ct);
    
    Task ReleaseAsync(string signalId,  CancellationToken ct);
}
