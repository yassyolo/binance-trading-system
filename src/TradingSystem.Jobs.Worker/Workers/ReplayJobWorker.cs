using Microsoft.Extensions.Options;
using TradingSystem.ReplayEngine;

namespace TradingSystem.Jobs.Worker.Workers;

public sealed class ReplayJobWorker(
    IReplayJobStore jobs, 
    ReplayEngine.ReplayEngine engine, 
    IOptions<JobWorkerOptions> options, 
    ILogger<ReplayJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var settings  =  options.Value;
        
        if (!settings.Enabled) 
            return;
        var workerId  =  $"replay:{Environment.MachineName}:{Guid.NewGuid():N}";
       
        using var timer  =  new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1,  settings.PollSeconds)));
        do
        {
            var claimed  =  await jobs.ClaimAsync(workerId,  settings.BatchSize,  TimeSpan.FromMinutes(settings.ProcessingTimeoutMinutes),  ct);
            
            foreach (var job in claimed)
                await ProcessAsync(job,  ct);
        } 
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task ProcessAsync(ReplayJob job,  CancellationToken ct)
    {
        try
        {
            await engine.RunAsync(job,  null,  ct);
        }
        catch (OperationCanceledException)
        {
            if (ct.IsCancellationRequested) 
                return;
            
            if (await jobs.IsCancellationRequestedAsync(job.ReplayId,  CancellationToken.None))
            {
                await jobs.MarkCancelledAsync(job.ReplayId,  CancellationToken.None);
                
                logger.LogInformation("Replay {ReplayId} was cancelled.",  job.ReplayId);
                
                return;
            }
            
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,  "Replay {ReplayId} failed.",  job.ReplayId);
            
            await jobs.FailAsync(job.ReplayId,  exception.Message,  CancellationToken.None);
        }
    }
}
