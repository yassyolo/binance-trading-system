using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Operations.Configuration;
using TradingSystem.Operations.Contracts;

namespace TradingSystem.Operations.Workers;

public sealed class AlertEngineWorker(
    IEnumerable<IAlertCandidateSource> alertCandidates,
    IAlertStore alertStore,
    IOptions<AlertEngineOptions> options,
    ILogger<AlertEngineWorker> logger) 
    : BackgroundService
{
    private readonly AlertEngineOptions _options = options.Value;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _options.PollSeconds)));
        try
        {
            do
            {
                foreach (var candidate in alertCandidates)
                {
                    try
                    {
                        var alerts = await candidate.LoadAsync(ct);
                       
                        foreach (var alert in alerts)
                            await alertStore.UpsertActiveAsync(alert, ct);
                       
                        await alertStore.ResolveMissingAsync(candidate.SourcePrefix, alerts.Select(x => x.DeduplicationKey).ToArray(), ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Alert candidate {Source} failed", candidate.SourcePrefix);
                    }
                }
            } while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        { }
    }
}