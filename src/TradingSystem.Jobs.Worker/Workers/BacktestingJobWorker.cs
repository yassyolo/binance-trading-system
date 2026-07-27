using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Dashboard.Contracts;
using TradingSystem.JobOrchestration;
using TradingSystem.Jobs.Worker.Execution;

namespace TradingSystem.Jobs.Worker.Workers;

public sealed class BacktestingJobWorker(
    IDashboardJobQueue queue,
    BacktestExecutionService executor,
    IOptions<JobWorkerOptions> options,
    ILogger<BacktestingJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return;

        var workerId = $"backtest:{Environment.MachineName}:{Guid.NewGuid():N}";
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, settings.PollSeconds)));
        try
        {
            do
            {
                var jobs = await queue.ClaimAsync("Backtest", workerId, settings.BatchSize, TimeSpan.FromMinutes(settings.ProcessingTimeoutMinutes), stoppingToken);
                foreach (var job in jobs)
                    await ProcessAsync(job, settings, stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal hosted-service shutdown; do not mark the current job as failed.
        }
    }

    private async Task ProcessAsync(DashboardJob job, JobWorkerOptions settings, CancellationToken cancellationToken)
    {
        try
        {
            await queue.ReportProgressAsync(job.JobId, 5, "Loading historical data", cancellationToken);
            var request = JsonSerializer.Deserialize<BacktestRequest>(job.RequestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("Invalid backtest request.");
            var runId = await executor.ExecuteAsync(request, settings.Interval, cancellationToken);
            await queue.CompleteAsync(job.JobId, runId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Backtest job {JobId} failed", job.JobId);
            var retrySeconds = Math.Min(300, Math.Pow(2, Math.Max(1, job.AttemptCount)));
            await queue.FailAsync(job.JobId, exception.Message, settings.MaximumAttempts, TimeSpan.FromSeconds(retrySeconds), cancellationToken);
        }
    }
}
