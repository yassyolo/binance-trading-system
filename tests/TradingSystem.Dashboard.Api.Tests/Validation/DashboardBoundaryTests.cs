using TradingSystem.Dashboard.Api.Exceptions;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Contracts.Models.Backtesting;
using TradingSystem.Dashboard.Contracts.Models.Bots;
using TradingSystem.Dashboard.Contracts.Models.Enums;
using TradingSystem.Dashboard.Contracts.Models.Optimization;
using TradingSystem.ReplayEngine.Models;
using TradingSystem.ReplayEngine.Models.Enums;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Validation;

public sealed class DashboardBoundaryTests
{
    [Theory]
    [InlineData(101)]
    [InlineData(-1)]
    public void BotConfiguration_OrderSideLimitOutsidePublicRange_ShouldBeRejected(int value)
    {
        var request = ValidConfiguration() with { OrderSideLimit = value };

        Assert.Throws<ApiValidationException>(() => RequestValidation.Validate(request));
    }

    [Fact]
    public void BotConfiguration_CooldownAboveOneDay_ShouldBeRejected()
    {
        var request = ValidConfiguration() with { CooldownSeconds = 86_401 };

        Assert.Throws<ApiValidationException>(() => RequestValidation.Validate(request));
    }

    [Theory]
    [InlineData("BTC-USDC")]
    [InlineData("BTC/USDC")]
    [InlineData("BTC USDC")]
    public void BotConfiguration_InvalidSymbolCharacters_ShouldBeRejected(string symbol)
    {
        var request = ValidConfiguration() with { Symbol = symbol };

        Assert.Throws<ApiValidationException>(() => RequestValidation.Validate(request));
    }

    [Theory]
    [InlineData(10.0001)]
    [InlineData(-0.0001)]
    public void Backtest_CommissionOutsidePublicRange_ShouldBeRejected(double value)
    {
        var request = ValidBacktest() with { CommissionPercent = (decimal)value };

        Assert.Throws<ApiValidationException>(() => RequestValidation.Validate(request));
    }

    [Fact]
    public void Backtest_PeriodAboveTenYears_ShouldBeRejected()
    {
        var to = DateTime.UtcNow;
        var request = ValidBacktest() with
        {
            FromUtc = to.AddDays(-3661),
            ToUtc = to
        };

        Assert.Throws<ApiValidationException>(() => RequestValidation.Validate(request));
    }

    [Fact]
    public void Optimization_MoreThanTwentyRanges_ShouldBeRejected()
    {
        var ranges = Enumerable.Range(1, 21)
            .Select(x => new OptimizationRangeDto($"P{x}", 1, 2, 1))
            .ToArray();

        var request = ValidOptimization() with { Ranges = ranges };

        Assert.Throws<ApiValidationException>(() => RequestValidation.Validate(request));
    }

    [Fact]
    public void Optimization_ReversedRange_ShouldBeRejected()
    {
        var request = ValidOptimization() with
        {
            Ranges = [new OptimizationRangeDto("ProfitDistance", 300, 100, 10)]
        };

        Assert.Throws<ApiValidationException>(() => RequestValidation.Validate(request));
    }

    [Fact]
    public void Optimization_TopResultsAboveMaximum_ShouldBeRejected()
    {
        var request = ValidOptimization() with { TopResults = 1001 };

        Assert.Throws<ApiValidationException>(() => RequestValidation.Validate(request));
    }

    [Fact]
    public void StrategyComparison_WithCandidatePluginAndVersion_ShouldBeAccepted()
    {
        var request = new CreateReplayRequest(
            "Compare",
            ReplayMode.StrategyComparison,
            CandidateStrategyPluginId: "candidate-plugin",
            CandidateStrategyVersion: "2.0.0");

        RequestValidation.Validate(request);
    }

    [Fact]
    public void Chart_Exactly366Days_ShouldBeAccepted()
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-366);

        RequestValidation.ValidateChart("BTCUSDC", "1d", from, to);
    }

    [Fact]
    public void PageSize_CustomMaximumBoundary_ShouldBeAccepted()
    {
        Assert.Equal(100, RequestValidation.PageSize(100, maximum: 100));
    }

    private static UpdateBotConfigurationRequest ValidConfiguration()
        => new(1, "Grid", "BTCUSDC", TradingEnvironment.Demo, "Internal", true, true, 0.001m, 10, 100m, 50m, 2, 60, "test");

    private static BacktestRequest ValidBacktest()
        => new("BOT8012", "BTCUSDC", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 10_000m, "Internal", 0.04m, 0.01m, new Dictionary<string, string>());

    private static OptimizationRequest ValidOptimization()
        => new(
            "BOT8012",
            "BTCUSDC",
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow,
            10_000m,
            "Internal",
            [new OptimizationRangeDto("ProfitDistance", 100m, 300m, 100m)],
            10,
            false,
            null,
            null,
            null);
}
