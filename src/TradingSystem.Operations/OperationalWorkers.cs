using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        var o = options.Value;if(!o.Enabled)return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(2, o.IntervalSeconds)));
        do
        {
            try
            {
                var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()??"unknown";
                var details = new Dictionary<string, string>{{"processId", Environment.ProcessId.ToString()}, {"machine", Environment.MachineName}};
                await store.UpsertAsync(new(o.ServiceName, _instanceId, version, o.Environment, OperationalStatus.Healthy, _startedAtUtc, DateTime.UtcNow, Math.Max(o.StaleAfterSeconds, o.IntervalSeconds*2), details), stoppingToken);
            }
            catch(Exception ex){logger.LogError(ex, "Failed to publish heartbeat for {ServiceName}", o.ServiceName);}
        } while(await timer.WaitForNextTickAsync(stoppingToken));
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
        var o = options.Value;if(!o.Enabled)return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, o.PollSeconds)));
        do
        {
            foreach(var source in sources)
            {
                try
                {
                    var alerts = await source.LoadAsync(stoppingToken);
                    foreach(var alert in alerts)await store.UpsertActiveAsync(alert, stoppingToken);
                    await store.ResolveMissingAsync(source.SourcePrefix, alerts.Select(x => x.DeduplicationKey).ToArray(), stoppingToken);
                }
                catch(Exception ex){logger.LogError(ex, "Alert source {Source} failed", source.SourcePrefix);}
            }
        } while(await timer.WaitForNextTickAsync(stoppingToken));
    }
}
