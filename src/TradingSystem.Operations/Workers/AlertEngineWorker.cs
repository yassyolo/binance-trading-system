using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Operations.Configuration;
using TradingSystem.Operations.Contracts;

namespace TradingSystem.Operations.Workers;

public sealed class AlertEngineWorker(
    IEnumerable<IAlertCandidateSource> sources,
    IAlertStore store,
    IOptions<AlertEngineOptions> options,
    ILogger<AlertEngineWorker> logger) 
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, settings.PollSeconds)));
        try
        {
            do
            {
                foreach (var source in sources)
                {
                    try
                    {
                        var alerts = await source.LoadAsync(stoppingToken);
                        foreach (var alert in alerts)
                            await store.UpsertActiveAsync(alert, stoppingToken);
                       
                        await store.ResolveMissingAsync(source.SourcePrefix, alerts.Select(x => x.DeduplicationKey).ToArray(), stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Alert source {Source} failed", source.SourcePrefix);
                    }
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        { }
    }
}