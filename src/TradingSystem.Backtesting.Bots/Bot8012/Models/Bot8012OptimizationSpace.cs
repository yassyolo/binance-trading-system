namespace TradingSystem.Backtesting.Bots.Bot8012.Models;

public sealed record Bot8012OptimizationSpace
{
    public IReadOnlyList<decimal> ProfitDistances { get; init; } = [100m, 150m, 200m, 250m, 300m];
    public IReadOnlyList<decimal> PriceDistances { get; init; } = [200m, 300m, 400m, 500m, 600m];
    public IReadOnlyList<int> SideLimits { get; init; } = [1, 2, 3];
    public IReadOnlyList<int> Cooldowns { get; init; } = [0, 60, 180, 300];
}
