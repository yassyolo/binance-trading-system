using Microsoft.Extensions.Options;
using TradingSystem.Reconciliation.Configuration;
using TradingSystem.Reconciliation.Services;

namespace StrategyService.Reliability;

public sealed class ReconciliationWorker(
    IOptions<ReconciliationOptions> options,
    PositionReconciliationService service,
    ILogger<ReconciliationWorker> logger) 
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.Value.IntervalSeconds)));

        do
        {
            try
            {
                var result = await service.RunAsync(stoppingToken);

                logger.LogInformation("Reconciliation completed. Findings = {Findings}, Healed = {Healed}", result.Findings.Count, result.HealedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Reconciliation cycle failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
