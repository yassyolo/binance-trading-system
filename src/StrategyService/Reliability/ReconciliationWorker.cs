using Microsoft.Extensions.Options;
using TradingSystem.Reconciliation.Configuration;
using TradingSystem.Reconciliation.Services;

namespace StrategyService.Reliability;

public sealed class ReconciliationWorker(
    IOptions<ReconciliationOptions> options,
    PositionReconciliationService positionReconciliation,
    ILogger<ReconciliationWorker> logger) 
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!options.Value.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.Value.IntervalSeconds)));

        do
        {
            try
            {
                var result = await positionReconciliation.RunAsync(ct);

                logger.LogInformation("Reconciliation completed. Findings = {Findings}, Healed = {Healed}", result.Findings.Count, result.HealedCount);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reconciliation cycle failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }
}
