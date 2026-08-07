namespace TradingSystem.Backtesting.Bots.Bot8011.Models;

public sealed record Bot8011OptimizationSpace
{
    public IReadOnlyList<decimal> TakeProfitPercents { get; init; } = [0.08m, 0.12m, 0.16m];

    public IReadOnlyList<decimal> StopLossDistances { get; init; } = [200m, 300m, 400m];

    public IReadOnlyList<decimal> Stop3EntryOffsets { get; init; } = [0m, 50m];

    public IReadOnlyList<decimal> Stop3TrailingSteps { get; init; } = [200m, 400m, 600m];

    public IReadOnlyList<decimal> Stop3TrailingBuffers { get; init; } = [25m, 50m, 100m];

    public IReadOnlyList<int> Cooldowns { get; init; } = [0, 180, 300];
}
