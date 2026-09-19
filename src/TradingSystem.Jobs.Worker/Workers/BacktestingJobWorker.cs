using Microsoft.Extensions.Options;
using System.Text.Json;
using TradingSystem.Dashboard.Contracts.Models.Backtesting;
using TradingSystem.JobOrchestration.Contracts;
using TradingSystem.JobOrchestration.Models;
using TradingSystem.Jobs.Worker.Configuration;
using TradingSystem.Jobs.Worker.Exceptions;
using TradingSystem.Jobs.Worker.Execution;

namespace TradingSystem.Jobs.Worker.Workers;

public sealed class BacktestingJobWorker(
    IDashboardJobQueue jobQueue,
    BacktestExecutionService backtestExecutor,
    IOptions<JobWorkerOptions> jobOptions,
    ILogger<BacktestingJobWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var options = jobOptions.Value;
        if (!options.Enabled)
            return;

        var workerId = $"backtest:{Environment.MachineName}:{Guid.NewGuid():N}";
        var pollDelay = TimeSpan.FromSeconds(Math.Max(1, options.PollSeconds));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var jobs = await jobQueue.ClaimAsync("Backtest", workerId, options.BatchSize, TimeSpan.FromMinutes(options.ProcessingTimeoutMinutes), ct);

                foreach (var job in jobs)
                    await ProcessSafelyAsync(job, options, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Backtest worker polling cycle failed. Worker = {WorkerId}. The worker will retry.", workerId);
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
            await jobQueue.ReportProgressAsync(job.JobId, 5, "Loading historical data", ct);

            var request = JsonSerializer.Deserialize<BacktestRequest>(job.RequestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new ArgumentException("Invalid backtest request.");

            var runId = await backtestExecutor.ExecuteAsync(request, settings.Interval, ct);
            
            await jobQueue.CompleteAsync(job.JobId, runId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Backtest job {JobId} failed.", job.JobId);

            try
            {
                var maximumAttempts = IsPermanent(ex) ? job.AttemptCount : settings.MaximumAttempts;

                var retrySeconds = Math.Min(300, Math.Pow(2, Math.Max(1, job.AttemptCount)));

                await jobQueue.FailAsync(job.JobId, ex.ToString(), maximumAttempts, TimeSpan.FromSeconds(retrySeconds), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception persistenceEx)
            {
                logger.LogError(persistenceEx, "Could not persist failure for backtest job {JobId}. It will be reclaimed after the processing timeout.", job.JobId);
            }
        }
    }

    private static bool IsPermanent(Exception ex) 
        => ex is ArgumentException
            or NotSupportedException
            or JsonException
            or HistoricalDataUnavailableException;
}
