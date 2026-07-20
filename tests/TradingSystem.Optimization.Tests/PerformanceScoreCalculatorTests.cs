using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Optimization.Models;
using TradingSystem.Optimization.Scoring;

namespace TradingSystem.Optimization.Tests;

public sealed class PerformanceScoreCalculatorTests
{
    [Fact]
    public void Calculate_ShouldPenalizeLargeDrawdown()
    {
        var calculator = new PerformanceScoreCalculator();
        var lowDrawdown = new BotBacktestMetrics { ReturnPercent = 10m, ProfitFactor = 2m, MaximumDrawdownPercent = 5m, ClosedPositions = 20 };
        var highDrawdown = lowDrawdown with { MaximumDrawdownPercent = 25m };
        Assert.True(calculator.Calculate(lowDrawdown, new OptimizationScoreWeights()) > calculator.Calculate(highDrawdown, new OptimizationScoreWeights()));
    }
}
