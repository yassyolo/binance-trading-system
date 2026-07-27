using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Dashboard.Contracts;
using TradingSystem.JobOrchestration;
using TradingSystem.Jobs.Worker.Execution;

namespace TradingSystem.Jobs.Worker.Workers;

public sealed class OptimizationJobWorker(
    IDashboardJobQueue queue,
    OptimizationExecutionService executor,
    IOptions<JobWorkerOptions> options,
    ILogger<OptimizationJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return;

        var workerId = $"optimization:{Environment.MachineName}:{Guid.NewGuid():N}";
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, settings.PollSeconds)));
        try
        {
            do
            {
                var jobs = await queue.ClaimAsync("Optimization", workerId, 1, TimeSpan.FromMinutes(settings.ProcessingTimeoutMinutes), stoppingToken);
                foreach (var job in jobs)
                    await ProcessAsync(job, settings, stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal hosted-service shutdown.
        }
    }

    private async Task ProcessAsync(DashboardJob job, JobWorkerOptions settings, CancellationToken cancellationToken)
    {
        try
        {
            await queue.ReportProgressAsync(job.JobId, 5, "Preparing parameter combinations", cancellationToken);
            var request = JsonSerializer.Deserialize<OptimizationRequest>(job.RequestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("Invalid optimization request.");
            var runId = await executor.ExecuteAsync(request, settings.Interval, cancellationToken);
            await queue.CompleteAsync(job.JobId, runId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Optimization job {JobId} failed", job.JobId);
            var retrySeconds = Math.Min(300, Math.Pow(2, Math.Max(1, job.AttemptCount)));
            await queue.FailAsync(job.JobId, exception.Message, settings.MaximumAttempts, TimeSpan.FromSeconds(retrySeconds), cancellationToken);
        }
    }
}
