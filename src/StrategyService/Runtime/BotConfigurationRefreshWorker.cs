using Microsoft.Extensions.Options;
using TradingSystem.BotRuntime.Configuration;

namespace StrategyService.Runtime;

public sealed class BotConfigurationRefreshWorker(
    IBotRuntimeConfigurationStore store, 
    IBotRuntimeConfigurationProvider provider, 
    IOptions<BotRuntimeOptions> options, 
    ILogger<BotConfigurationRefreshWorker> logger) : BackgroundService
{
    private readonly BotRuntimeOptions _options  =  options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        var watermark  =  DateTime.UnixEpoch;
        using var timer  =  new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1,  _options.ConfigurationRefreshSeconds)));
        do
        {
            try
            {
                var changed  =  await store.GetChangedSinceAsync(watermark,  stoppingToken);
                foreach (var configuration in changed)
                {
                    provider.Set(configuration);
                    if (configuration.UpdatedAtUtc > watermark)
                        watermark  =  configuration.UpdatedAtUtc;
                    logger.LogInformation("Bot configuration refreshed. Bot = {Bot} Version = {Version} RestartRequired = {RestartRequired}",  configuration.BotName,  configuration.Version,  configuration.RestartRequired);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,  "Dynamic bot configuration refresh failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
