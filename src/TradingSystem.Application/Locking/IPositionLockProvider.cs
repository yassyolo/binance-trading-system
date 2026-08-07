namespace TradingSystem.Application.Locking;

public interface IPositionLockProvider
{
    Task<IAsyncDisposable?> TryAcquireAsync(string botName, string positionId, TimeSpan ttl, CancellationToken ct);
}
