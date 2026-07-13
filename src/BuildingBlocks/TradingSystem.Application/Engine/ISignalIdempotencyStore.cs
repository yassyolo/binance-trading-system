namespace TradingSystem.Application.Engine;

public interface ISignalIdempotencyStore
{
    Task<bool> TryStartAsync(
        string signalId,
        TimeSpan processingTtl,
        CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        string signalId,
        TimeSpan completedTtl,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        string signalId,
        CancellationToken cancellationToken);
}
