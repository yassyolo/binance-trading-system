using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Operations.Enums;

namespace TradingSystem.Operations;

public sealed class ServiceHeartbeatWorker(
    IServiceHeartbeatStore store,
    IOptions<ServiceHeartbeatOptions> options,
    ILogger<ServiceHeartbeatWorker> logger) : BackgroundService
{
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;
    private readonly string _instanceId = $"{Environment.MachineName}-{Environment.ProcessId}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(2, settings.IntervalSeconds)));
        try
        {
            do
            {
                try
                {
                    var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
                    var details = new Dictionary<string, string>
                    {
                        ["processId"] = Environment.ProcessId.ToString(),
                        ["machine"] = Environment.MachineName
                    };
                    await store.UpsertAsync(new ServiceHeartbeat(
                        settings.ServiceName,
                        _instanceId,
                        version,
                        settings.Environment,
                        OperationalStatus.Healthy,
                        _startedAtUtc,
                        DateTime.UtcNow,
                        Math.Max(settings.StaleAfterSeconds, settings.IntervalSeconds * 2),
                        details), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to publish heartbeat for {ServiceName}", settings.ServiceName);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }
}

public sealed class AlertEngineWorker(
    IEnumerable<IAlertCandidateSource> sources,
    IAlertStore store,
    IOptions<AlertEngineOptions> options,
    ILogger<AlertEngineWorker> logger) : BackgroundService
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
        {
            // Normal shutdown.
        }
    }
}