using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.PortfolioManagement.Provider;
using TradingSystem.Prometheus.Configuration;
using TradingSystem.Prometheus.PrometheusMetrics;

namespace TradingSystem.Prometheus.Workers;

public sealed class PortfolioMetricsWorker(
    IPortfolioSnapshotProvider portfolio,
    TradingMetrics metrics,
    IOptions<PrometheusOptions> options,
    ILogger<PortfolioMetricsWorker> logger) : BackgroundService
{
    private readonly PrometheusOptions _options = options.Value;
    private readonly HashSet<(string Bot, string Symbol, string Side)> _knownPositionLabels = [];
    private readonly HashSet<string> _knownSymbols = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.PortfolioRefreshSeconds));

        do
        {
            try
            {
                var snapshot = await portfolio.GetSnapshotAsync(stoppingToken);
                
                metrics.PortfolioEquity.Set((double)snapshot.Equity);
                metrics.PortfolioUnrealizedPnl.Set((double)snapshot.UnrealizedPnl);
                metrics.PortfolioDailyRealizedPnl.Set((double)snapshot.RealizedPnlToday);
                metrics.PortfolioDrawdownPercent.Set((double)snapshot.DailyDrawdownPercent);
                metrics.GrossNotional.Set((double)snapshot.GrossNotional);

                var currentLabels = snapshot.Positions
                    .GroupBy(x => new { x.BotName, x.Symbol, Side = x.Side.ToString() })
                    .ToDictionary(
                        x => (x.Key.BotName, x.Key.Symbol, x.Key.Side),
                        x => x.Count());

                foreach (var stale in _knownPositionLabels.Except(currentLabels.Keys).ToArray())
                    metrics.OpenPositions.WithLabels(stale.Item1, stale.Symbol, stale.Side).Set(0);

                foreach (var item in currentLabels)
                {
                    metrics.OpenPositions.WithLabels(item.Key.BotName, item.Key.Symbol, item.Key.Side).Set(item.Value);
                    
                    _knownPositionLabels.Add(item.Key);
                }

                var currentSymbols = snapshot.Symbols.ToDictionary(x => x.Symbol, StringComparer.OrdinalIgnoreCase);
                foreach (var stale in _knownSymbols.Except(currentSymbols.Keys, StringComparer.OrdinalIgnoreCase).ToArray())
                    metrics.NetNotionalBySymbol.WithLabels(stale).Set(0);

                foreach (var symbol in currentSymbols.Values)
                {
                    metrics.NetNotionalBySymbol.WithLabels(symbol.Symbol).Set((double)symbol.NetNotional);
                    _knownSymbols.Add(symbol.Symbol);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                metrics.ProcessingFailures.WithLabels("portfolio_metrics", "-", ex.GetType().Name).Inc();
                
                logger.LogError(ex, "Prometheus portfolio refresh failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
