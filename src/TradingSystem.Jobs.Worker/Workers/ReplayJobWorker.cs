using Microsoft.Extensions.Options;
using TradingSystem.Jobs.Worker.Configuration;
using TradingSystem.ReplayEngine.Models;
using TradingSystem.ReplayEngine.Store;

namespace TradingSystem.Jobs.Worker.Workers;

public sealed class ReplayJobWorker(
	IReplayJobStore replayJobs,
	ReplayEngine.Engine.ReplayEngine engine,
	IOptions<JobWorkerOptions> options,
	ILogger<ReplayJobWorker> logger)
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		var settings = options.Value;
		if (!settings.Enabled)
			return;

		var workerId = $"replay:{Environment.MachineName}:{Guid.NewGuid():N}";
		var pollDelay = TimeSpan.FromSeconds(Math.Max(1, settings.PollSeconds));

		while (!ct.IsCancellationRequested)
		{
			try
			{
				var claimed = await replayJobs.ClaimAsync(workerId, settings.BatchSize, TimeSpan.FromMinutes(settings.ProcessingTimeoutMinutes), ct);

				foreach (var job in claimed)
					await ProcessSafelyAsync(job, ct);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Replay worker polling cycle failed. Worker = {WorkerId}. The worker will retry.", workerId);
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

	private async Task ProcessSafelyAsync(ReplayJob job, CancellationToken ct)
	{
		try
		{
			await engine.RunAsync(job, null, ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (OperationCanceledException)
		{
			try
			{
				if (await replayJobs.IsCancellationRequestedAsync(job.ReplayId, CancellationToken.None))
				{
					await replayJobs.MarkCancelledAsync(job.ReplayId, CancellationToken.None);

					logger.LogInformation("Replay {ReplayId} was cancelled.", job.ReplayId);
				}
			}
			catch (Exception persistenceEx)
			{
				logger.LogError(persistenceEx, "Could not persist cancellation for replay {ReplayId}.", job.ReplayId);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Replay {ReplayId} failed.", job.ReplayId);

			try
			{
				await replayJobs.FailAsync(job.ReplayId, ex.ToString(), CancellationToken.None);
			}
			catch (Exception persistenceEx)
			{
				logger.LogError(persistenceEx, "Could not persist failure for replay {ReplayId}. It will be reclaimed after the processing timeout.", job.ReplayId);
			}
		}
	}
}
