namespace TradingSystem.Application.Risk.Contracts;

public interface IRiskAdmissionLifecycle
{
    Task CompleteAsync(string signalId, bool executionSucceeded, CancellationToken ct);
}
