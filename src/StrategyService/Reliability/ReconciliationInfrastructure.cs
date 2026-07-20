using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8011;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Reconciliation;

namespace StrategyService.Reliability;

public sealed class BinanceExchangeStateProvider(IBinanceFuturesOrderClient client) : IExchangeStateProvider
{
    public async Task<ExchangeStateSnapshot> GetAsync(string symbol,  CancellationToken ct)
    {
        var positionsTask = client.GetPositionRiskAsync(symbol, ct);
        var normalTask = client.GetOpenOrdersAsync(symbol, ct);
        var algoTask = client.GetOpenAlgoOrdersAsync(symbol, ct);
        await Task.WhenAll(positionsTask, normalTask, algoTask);

        // Fix: Use '==' for comparison, not assignment, and remove '!' which is not valid here.
        var positions = positionsTask.Result
            .Where(x => x.PositionAmount != 0)
            .Select(x => new ExchangePositionSnapshot(x.Symbol, x.PositionSide, Math.Abs(x.PositionAmount), x.EntryPrice))
            .ToArray();

        var normal = normalTask.Result.Select(x => new ExchangeOrderSnapshot(x.Symbol, x.ClientOrderId, x.Type, x.Quantity, null));
        var algo = algoTask.Result.Select(x => new ExchangeOrderSnapshot(x.Symbol, x.ClientAlgoId, x.OrderType, x.Quantity, x.TriggerPrice));
        return new ExchangeStateSnapshot(positions, normal.Concat(algo).ToArray());
    }
}

public sealed class SafeHealingActionExecutor(IPositionStore store) : IHealingActionExecutor
{
    public async Task<bool> ExecuteAsync(ReconciliationFinding finding,  CancellationToken ct)
    {
        if (finding.SuggestedAction !=  HealingActionType.DeleteStaleLocalPosition  ||  finding.ShortId is null) return false;
        await store.DeleteAsync(finding.BotName, finding.ShortId, ct); return true;
    }
}

public sealed class ReconciliationWorker(IOptions<ReconciliationOptions> options,  PositionReconciliationService service,  ILogger<ReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.Value.IntervalSeconds)));
        do { try { var result = await service.RunAsync(stoppingToken); logger.LogInformation("Reconciliation completed. Findings = {Findings},  Healed = {Healed}", result.Findings.Count, result.HealedCount); }
             catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
             catch(Exception ex) { logger.LogError(ex, "Reconciliation cycle failed."); }
        } while(await timer.WaitForNextTickAsync(stoppingToken));
    }
}
