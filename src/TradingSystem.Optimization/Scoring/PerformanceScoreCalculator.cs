using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Optimization.Models;

namespace TradingSystem.Optimization.Scoring;

public sealed class PerformanceScoreCalculator
{
    public decimal Calculate(BotBacktestMetrics metrics,  OptimizationScoreWeights weights)
    {
        var profitFactor  =  Math.Min(metrics.ProfitFactor,  weights.MaximumProfitFactorContribution);
        var activityPenalty  =  Math.Max(0,  weights.MinimumClosedPositions - metrics.ClosedPositions) * weights.LowActivityPenalty;
        return metrics.ReturnPercent * weights.ReturnWeight
               + profitFactor * weights.ProfitFactorWeight
               - metrics.MaximumDrawdownPercent * weights.DrawdownPenalty
               - activityPenalty;
    }
}
