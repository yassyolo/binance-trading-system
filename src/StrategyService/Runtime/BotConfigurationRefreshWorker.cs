using Microsoft.Extensions.Options;
using TradingSystem.BotRuntime.Configuration;

namespace StrategyService.Runtime;

public sealed class BotConfigurationRefreshWorker(
    IBotRuntimeConfigurationStore store,
    IBotRuntimeConfigurationProvider provider,
    IOptions<BotRuntimeOptions> options,
    ILogger<BotConfigurationRefreshWorker> logger) : BackgroundService
{
    private readonly BotRuntimeOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;

        var watermark = DateTime.UnixEpoch;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.ConfigurationRefreshSeconds));
        do
        {
            try
            {
                var queryFrom = watermark == DateTime.UnixEpoch ? watermark : watermark.AddMilliseconds(-1);
                var changed = await store.GetChangedSinceAsync(queryFrom, stoppingToken);
                
                foreach (var configuration in changed.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.BotName, StringComparer.OrdinalIgnoreCase))
                {
                    provider.Set(configuration);
                    if (configuration.UpdatedAtUtc > watermark)
                        watermark = configuration.UpdatedAtUtc;

                    logger.LogInformation("Bot configuration refreshed. Bot = {Bot} Version = {Version} RestartRequired = {RestartRequired}", configuration.BotName, configuration.Version, configuration.RestartRequired);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Dynamic bot configuration refresh failed."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
