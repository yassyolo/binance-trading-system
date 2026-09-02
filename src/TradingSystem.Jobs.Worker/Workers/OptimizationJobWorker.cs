using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Dashboard.Contracts;
using TradingSystem.Dashboard.Contracts.Models.Optimization;
using TradingSystem.JobOrchestration.Contracts;
using TradingSystem.JobOrchestration.Models;
using TradingSystem.Jobs.Worker.Configuration;
using TradingSystem.Jobs.Worker.Execution;

namespace TradingSystem.Jobs.Worker.Workers;

public sealed class OptimizationJobWorker(
	IDashboardJobQueue queue,
	OptimizationExecutionService executor,
	IOptions<JobWorkerOptions> options,
	ILogger<OptimizationJobWorker> logger)
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		var settings = options.Value;
		if (!settings.Enabled)
			return;

		var workerId = $"optimization:{Environment.MachineName}:{Guid.NewGuid():N}";
		var pollDelay = TimeSpan.FromSeconds(Math.Max(1, settings.PollSeconds));

		while (!ct.IsCancellationRequested)
		{
			try
			{
				var jobs = await queue.ClaimAsync("Optimization", workerId, 1, TimeSpan.FromMinutes(settings.ProcessingTimeoutMinutes), ct);

				foreach (var job in jobs)
					await ProcessSafelyAsync(job, settings, ct);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception exception)
			{
				logger.LogError(exception, "Optimization worker polling cycle failed. Worker = {WorkerId}. The worker will retry.", workerId);
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
			await queue.ReportProgressAsync(job.JobId, 5, "Preparing parameter combinations", ct);

			var request = JsonSerializer.Deserialize<OptimizationRequest>(job.RequestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))
				?? throw new ArgumentException("Invalid optimization request.");

			var runId = await executor.ExecuteAsync(request, settings.Interval, ct);
			
			await queue.CompleteAsync(job.JobId, runId, ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Optimization job {JobId} failed.", job.JobId);

			try
			{
				var maximumAttempts = IsPermanent(ex) ? job.AttemptCount : settings.MaximumAttempts;

				var retrySeconds = Math.Min(300, Math.Pow(2, Math.Max(1, job.AttemptCount)));

				await queue.FailAsync(job.JobId, ex.ToString(), maximumAttempts, TimeSpan.FromSeconds(retrySeconds), ct);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception persistenceException)
			{
				logger.LogError(persistenceException, "Could not persist failure for optimization job {JobId}. It will be reclaimed after the processing timeout.", job.JobId);
			}
		}
	}

	private static bool IsPermanent(Exception ex) 
		=> ex is ArgumentException or NotSupportedException or JsonException;
}
