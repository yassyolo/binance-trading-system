using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Observability.Environment;
using TradingSystem.PortfolioManager;

namespace TradingSystem.HistoricalDatabase;

public sealed class HistoricalPortfolioSnapshotWorker(
    IPortfolioSnapshotProvider portfolio,
    IHistoricalEventStore store,
    ITradingEnvironmentProvider environment,
    IOptions<HistoricalDatabaseOptions> options,
    ILogger<HistoricalPortfolioSnapshotWorker> logger) : BackgroundService
{
    private readonly HistoricalDatabaseOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.PortfolioSnapshotIntervalSeconds));

        do
        {
            try
            {
                var snapshot = await portfolio.GetSnapshotAsync(stoppingToken);
                await store.AppendAsync(new HistoricalEvent(
                    Guid.NewGuid(),
                    HistoricalEventType.PortfolioSnapshot,
                    snapshot.GeneratedAtUtc,
                    environment.EnvironmentName,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "SNAPSHOT",
                    null,
                    null,
                    snapshot.RealizedPnlToday,
                    null,
                    new Dictionary<string, object?>
                    {
                        ["starting_equity"] = snapshot.StartingEquity,
                        ["equity"] = snapshot.Equity,
                        ["peak_equity"] = snapshot.PeakEquityToday,
                        ["unrealized_pnl"] = snapshot.UnrealizedPnl,
                        ["daily_drawdown"] = snapshot.DailyDrawdown,
                        ["daily_drawdown_percent"] = snapshot.DailyDrawdownPercent,
                        ["open_positions"] = snapshot.OpenPositions,
                        ["gross_notional"] = snapshot.GrossNotional,
                        ["net_notional"] = snapshot.NetNotional,
                        ["estimated_initial_margin"] = snapshot.EstimatedInitialMargin,
                        ["symbols"] = snapshot.Symbols,
                        ["bots"] = snapshot.Bots
                    }), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Historical portfolio snapshot failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

public sealed class HistoricalRetentionWorker(
    IHistoricalEventStore store,
    IOptions<HistoricalDatabaseOptions> options,
    TimeProvider timeProvider,
    ILogger<HistoricalRetentionWorker> logger) : BackgroundService
{
    private readonly HistoricalDatabaseOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.CleanupIntervalHours));
        do
        {
            try
            {
                var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-_options.RetentionDays);
                var deleted = await store.DeleteOlderThanAsync(cutoff, stoppingToken);
                logger.LogInformation(
                    "Historical retention completed. Cutoff = {Cutoff}, Deleted = {Deleted}",
                    cutoff,
                    deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Historical retention cycle failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
