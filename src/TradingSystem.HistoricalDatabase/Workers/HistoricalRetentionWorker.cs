using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.HistoricalDatabase.Configuration;
using TradingSystem.HistoricalDatabase.EventStore;

namespace TradingSystem.HistoricalDatabase.Workers;

public sealed class HistoricalRetentionWorker(
    IHistoricalEventStore historicalEventStore,
    IOptions<HistoricalDatabaseOptions> options,
    TimeProvider timeProvider,
    ILogger<HistoricalRetentionWorker> logger) 
    : BackgroundService
{
    private readonly HistoricalDatabaseOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.CleanupIntervalHours));
        do
        {
            try
            {
                var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-_options.RetentionDays);
                var deleted = await historicalEventStore.DeleteOlderThanAsync(cutoff, ct);
                
                logger.LogInformation("Historical retention completed. Cutoff = {Cutoff}, Deleted = {Deleted}", cutoff, deleted);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Historical retention cycle failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }
}
