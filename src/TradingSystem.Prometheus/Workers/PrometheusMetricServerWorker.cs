using Prometheus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Prometheus.Configuration;

namespace TradingSystem.Prometheus.Workers;

public sealed class PrometheusMetricServerWorker(
    IOptions<PrometheusOptions> options,
    ILogger<PrometheusMetricServerWorker> logger) 
    : BackgroundService
{
    private readonly PrometheusOptions _options = options.Value;
    private KestrelMetricServer? _server;

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        _server = new KestrelMetricServer(port: _options.Port, url: _options.Url);
        _server.Start();
       
        logger.LogInformation("Prometheus metrics endpoint started on port {Port}{Url}", _options.Port, _options.Url);
        
        return Task.Delay(Timeout.InfiniteTimeSpan, ct);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_server is not null)
            await _server.StopAsync();
        
        await base.StopAsync(ct);
    }

    public override void Dispose()
    {
        _server?.Dispose();
        base.Dispose();
    }
}