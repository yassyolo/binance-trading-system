using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using TradingSystem.Operations.Configuration;
using TradingSystem.Operations.Contracts;
using TradingSystem.Operations.Models;
using TradingSystem.Operations.Models.Enums;

namespace TradingSystem.Operations.Workers;

public sealed class ServiceHeartbeatWorker(
    IServiceHeartbeatStore heartbeatStore,
    IOptions<ServiceHeartbeatOptions> options,
    ILogger<ServiceHeartbeatWorker> logger) 
    : BackgroundService
{
    private readonly ServiceHeartbeatOptions _options = options.Value;
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;
    private readonly string _instanceId = $"{Environment.MachineName}-{Environment.ProcessId}";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(2, _options.IntervalSeconds)));
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
                    
                    await heartbeatStore.UpsertAsync(new ServiceHeartbeat(
                        _options.ServiceName,
                        _instanceId,
                        version,
                        _options.Environment,
                        OperationalStatus.Healthy,
                        _startedAtUtc,
                        DateTime.UtcNow,
                        Math.Max(_options.StaleAfterSeconds, _options.IntervalSeconds * 2),
                        details), 
                        ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to publish heartbeat for {ServiceName}", _options.ServiceName);
                }
            } while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {}
    }
}
