using TradingSystem.JobOrchestration.Models;

namespace TradingSystem.JobOrchestration.Contracts;

public interface IDashboardJobQueue
{
    Task<IReadOnlyList<DashboardJob>> ClaimAsync(string type, string workerId, int batchSize, TimeSpan processingTimeout, CancellationToken ct);

    Task ReportProgressAsync(Guid jobId, int percent, string stage, CancellationToken ct);

    Task CompleteAsync(Guid jobId, Guid runId, CancellationToken ct);

    Task FailAsync(Guid jobId, string error, int maxAttempts, TimeSpan retryDelay, CancellationToken ct);
}
