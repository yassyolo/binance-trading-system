using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Models;

namespace StrategyService.IntegrationExamples;

public sealed class IndicatorSnapshotPublisherExample(ISignalGenerationCoordinator coordinator)
{
    public Task OnIndicatorsReadyAsync(
        string symbol, string interval,
        DateTime candleOpenUtc, DateTime candleCloseUtc,
        decimal open, decimal high, decimal low, decimal close, decimal volume,
        decimal bollingerUpper, decimal bollingerLower,
        decimal smmaFast, decimal smmaSlow,
        decimal alligatorJaw, decimal alligatorTeeth, decimal alligatorLips,
        CancellationToken ct)
        => coordinator.ProcessAsync(new MarketIndicatorSnapshot(
            symbol, interval, candleOpenUtc, candleCloseUtc,
            open, high, low, close, volume,
            new Dictionary<string, decimal>
            {
                [IndicatorKeys.BollingerUpper] = bollingerUpper,
                [IndicatorKeys.BollingerLower] = bollingerLower,
                [IndicatorKeys.SmmaFast] = smmaFast,
                [IndicatorKeys.SmmaSlow] = smmaSlow,
                [IndicatorKeys.AlligatorJaw] = alligatorJaw,
                [IndicatorKeys.AlligatorTeeth] = alligatorTeeth,
                [IndicatorKeys.AlligatorLips] = alligatorLips
            }), ct);
}
