using Microsoft.Extensions.Options;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Reconciliation;
using TradingSystem.Reconciliation.Configuration;
using TradingSystem.Reconciliation.Enums;
using TradingSystem.Reconciliation.Executor;

namespace StrategyService.Reliability;

public sealed class BinanceExchangeStateProvider(
    IBinanceFuturesOrderClient client) : IExchangeStateProvider
{
    public async Task<ExchangeStateSnapshot> GetAsync(
        string symbol,
        CancellationToken ct)
    {
        var positionsTask = client.GetPositionRiskAsync(symbol, ct);
        var normalTask = client.GetOpenOrdersAsync(symbol, ct);
        var algoTask = client.GetOpenAlgoOrdersAsync(symbol, ct);

        await Task.WhenAll(positionsTask, normalTask, algoTask);

        var positions = (await positionsTask)
            .Where(x => x.PositionAmount != 0)
            .Select(x => new ExchangePositionSnapshot(
                x.Symbol,
                x.PositionSide,
                Math.Abs(x.PositionAmount),
                x.EntryPrice))
            .ToArray();

        var normal = (await normalTask)
            .Select(x => new ExchangeOrderSnapshot(
                x.Symbol,
                x.ClientOrderId,
                x.Type,
                x.Quantity,
                null));

        var algo = (await algoTask)
            .Select(x => new ExchangeOrderSnapshot(
                x.Symbol,
                x.ClientAlgoId,
                x.OrderType,
                x.Quantity,
                x.TriggerPrice));

        return new ExchangeStateSnapshot(
            positions,
            normal.Concat(algo).ToArray());
    }
}

public sealed class SafeHealingActionExecutor(
    ILogger<SafeHealingActionExecutor> logger) : IHealingActionExecutor
{
    public Task<bool> ExecuteAsync(
        ReconciliationFinding finding,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (finding.SuggestedAction != HealingActionType.DeleteStaleLocalPosition ||
            finding.ShortId is null)
        {
            return Task.FromResult(false);
        }

        logger.LogWarning(
            "Automatic healing refused destructive local-position deletion. Bot = {Bot}, Position = {Position}, Finding = {FindingId}. Manual review is required.",
            finding.BotName,
            finding.ShortId,
            finding.Id);

        return Task.FromResult(false);
    }
}

public sealed class ReconciliationWorker(
    IOptions<ReconciliationOptions> options,
    PositionReconciliationService service,
    ILogger<ReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(Math.Max(5, options.Value.IntervalSeconds)));

        do
        {
            try
            {
                var result = await service.RunAsync(stoppingToken);

                logger.LogInformation(
                    "Reconciliation completed. Findings = {Findings}, Healed = {Healed}",
                    result.Findings.Count,
                    result.HealedCount);
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
