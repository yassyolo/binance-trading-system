using Microsoft.Extensions.Options;
using StrategyService.Runtime.Configuration;
using TradingSystem.BotRuntime.Configuration.Contracts;

namespace StrategyService.Runtime;

public sealed class BotConfigurationRefreshWorker(
    IBotRuntimeConfigurationStore store,
    IBotRuntimeConfigurationProvider provider,
    IOptions<BotRuntimeOptions> options,
    ILogger<BotConfigurationRefreshWorker> logger) 
    : BackgroundService
{
    private readonly BotRuntimeOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled) return;

        var watermark = DateTime.UnixEpoch;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.ConfigurationRefreshSeconds));
        do
        {
            try
            {
                var queryFrom = watermark == DateTime.UnixEpoch ? watermark : watermark.AddMilliseconds(-1);
                var changed = await store.GetChangedSinceAsync(queryFrom, ct);
                
                foreach (var config in changed.OrderBy(x => x.UpdatedAtUtc).ThenBy(x => x.BotName, StringComparer.OrdinalIgnoreCase))
                {
                    provider.Set(config);
                    
                    if (config.UpdatedAtUtc > watermark)
                        watermark = config.UpdatedAtUtc;

                    logger.LogInformation("Bot config refreshed. Bot = {Bot} Version = {Version} RestartRequired = {RestartRequired}", config.BotName, config.Version, config.RestartRequired);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) 
            { 
                break; 
            }
            catch (Exception ex) 
            { 
                logger.LogError(ex, "Dynamic bot config refresh failed."); 
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }
}
