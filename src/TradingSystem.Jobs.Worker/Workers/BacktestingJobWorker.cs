using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Dashboard.Contracts;
using TradingSystem.JobOrchestration.Contracts;
using TradingSystem.JobOrchestration.Models;
using TradingSystem.Jobs.Worker.Configuration;
using TradingSystem.Jobs.Worker.Exceptions;
using TradingSystem.Jobs.Worker.Execution;

namespace TradingSystem.Jobs.Worker.Workers;

public sealed class BacktestingJobWorker(
    IDashboardJobQueue queue,
    BacktestExecutionService executor,
    IOptions<JobWorkerOptions> options,
    ILogger<BacktestingJobWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return;

        var workerId = $"backtest:{Environment.MachineName}:{Guid.NewGuid():N}";
        var pollDelay = TimeSpan.FromSeconds(Math.Max(1, settings.PollSeconds));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var jobs = await queue.ClaimAsync(
                    "Backtest",
                    workerId,
                    settings.BatchSize,
                    TimeSpan.FromMinutes(settings.ProcessingTimeoutMinutes),
                    ct);

                foreach (var job in jobs)
                    await ProcessSafelyAsync(job, settings, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Backtest worker polling cycle failed. Worker = {WorkerId}. The worker will retry.",
                    workerId);
            }

            try
            {
                await Task.Delay(pollDelay, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessSafelyAsync(DashboardJob job, JobWorkerOptions settings, CancellationToken ct)
    {
        try
        {
            await queue.ReportProgressAsync(job.JobId, 5, "Loading historical data", ct);

            var request = JsonSerializer.Deserialize<BacktestRequest>(
                job.RequestJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new ArgumentException("Invalid backtest request.");

            var runId = await executor.ExecuteAsync(request, settings.Interval, ct);
            await queue.CompleteAsync(job.JobId, runId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Backtest job {JobId} failed.", job.JobId);

            try
            {
                var maximumAttempts = IsPermanent(exception)
                    ? job.AttemptCount
                    : settings.MaximumAttempts;

                var retrySeconds = Math.Min(
                    300,
                    Math.Pow(2, Math.Max(1, job.AttemptCount)));

                await queue.FailAsync(
                    job.JobId,
                    FullError(exception),
                    maximumAttempts,
                    TimeSpan.FromSeconds(retrySeconds),
                    ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception persistenceException)
            {
                logger.LogError(
                    persistenceException,
                    "Could not persist failure for backtest job {JobId}. It will be reclaimed after the processing timeout.",
                    job.JobId);
            }
        }
    }

    private static bool IsPermanent(Exception exception) =>
        exception is ArgumentException
            or NotSupportedException
            or JsonException
            or HistoricalDataUnavailableException;

    private static string FullError(Exception exception) =>
        exception.ToString();
}
