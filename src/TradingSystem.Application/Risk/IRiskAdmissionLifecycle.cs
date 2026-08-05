namespace TradingSystem.Application.Risk;

/// <summary>
/// Completes the lifecycle of a risk admission reservation after execution.
/// The reservation is released immediately on failure. On success, the
/// portfolio snapshot is invalidated before release so the next admission
/// cannot rely on a stale cached snapshot.
/// </summary>
public interface IRiskAdmissionLifecycle
{
    Task CompleteAsync(
        string signalId,
        bool executionSucceeded,
        CancellationToken ct);
}
