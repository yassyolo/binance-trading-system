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

public sealed class RequestValidationTests
{
    [Fact]
    public void ValidBotConfiguration_ShouldBeAccepted()
    {
        RequestValidation.Validate(
            ValidBotConfiguration());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BotConfiguration_NonPositiveVersion_ShouldBeRejected(
        long version)
    {
        var request =
            ValidBotConfiguration() with
            {
                ExpectedVersion = version
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(126)]
    public void BotConfiguration_InvalidLeverage_ShouldBeRejected(
        int leverage)
    {
        var request =
            ValidBotConfiguration() with
            {
                Leverage = leverage
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void BotConfiguration_ZeroQuantity_ShouldBeRejected()
    {
        var request =
            ValidBotConfiguration() with
            {
                Quantity = 0
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void BotConfiguration_EmptyReason_ShouldBeRejected()
    {
        var request =
            ValidBotConfiguration() with
            {
                Reason = ""
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void DangerousCommand_WithoutConfirmation_ShouldBeRejected()
    {
        var request = new BotCommandRequest(
            BotCommandType.EmergencyStop,
            "Risk incident",
            false);

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Theory]
    [InlineData(BotCommandType.ClosePosition)]
    [InlineData(BotCommandType.CancelTakeProfit)]
    [InlineData(BotCommandType.RecreateTakeProfit)]
    public void PositionCommand_WithoutPositionId_ShouldBeRejected(
        BotCommandType command)
    {
        var request = new BotCommandRequest(
            command,
            "Position action",
            true,
            PositionId: null);

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void ValidDangerousCommand_ShouldBeAccepted()
    {
        RequestValidation.Validate(
            new BotCommandRequest(
                BotCommandType.EmergencyStop,
                "Risk incident",
                true));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(501)]
    public void PageSize_OutsideRange_ShouldBeRejected(
        int take)
    {
        Assert.Throws<ApiValidationException>(
            () => RequestValidation.PageSize(take));
    }

    [Fact]
    public void PageSize_Zero_ShouldReturnDefault()
    {
        Assert.Equal(
            100,
            RequestValidation.PageSize(0));
    }

    [Fact]
    public void PageSize_CustomMaximum_ShouldBeApplied()
    {
        Assert.Throws<ApiValidationException>(
            () => RequestValidation.PageSize(
                101,
                maximum: 100));
    }

    [Fact]
    public void NegativeSkip_ShouldBeRejected()
    {
        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Skip(-1));
    }

    [Theory]
    [InlineData("BOT8012")]
    [InlineData("bot-8012")]
    [InlineData("bot_8012")]
    public void ValidBotName_ShouldBeAccepted(
        string botName)
    {
        RequestValidation.ValidateBotName(botName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("BOT 8012")]
    [InlineData("BOT!8012")]
    public void InvalidBotName_ShouldBeRejected(
        string botName)
    {
        Assert.Throws<ApiValidationException>(
            () =>
                RequestValidation.ValidateBotName(
                    botName));
    }

    [Fact]
    public void ValidChart_ShouldBeAccepted()
    {
        RequestValidation.ValidateChart(
            "BTCUSDC",
            "5m",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow);
    }

    [Fact]
    public void UnsupportedChartInterval_ShouldBeRejected()
    {
        Assert.Throws<ApiValidationException>(
            () =>
                RequestValidation.ValidateChart(
                    "BTCUSDC",
                    "7m",
                    DateTime.UtcNow.AddHours(-1),
                    DateTime.UtcNow));
    }

    [Fact]
    public void ChartRangeAbove366Days_ShouldBeRejected()
    {
        Assert.Throws<ApiValidationException>(
            () =>
                RequestValidation.ValidateChart(
                    "BTCUSDC",
                    "1d",
                    DateTime.UtcNow.AddDays(-367),
                    DateTime.UtcNow));
    }

    [Fact]
    public void ValidBacktest_ShouldBeAccepted()
    {
        RequestValidation.Validate(
            ValidBacktest());
    }

    [Fact]
    public void Backtest_InvalidPeriod_ShouldBeRejected()
    {
        var now = DateTime.UtcNow;

        var request =
            ValidBacktest() with
            {
                FromUtc = now,
                ToUtc = now.AddMinutes(-1)
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void Backtest_ZeroInitialBalance_ShouldBeRejected()
    {
        var request =
            ValidBacktest() with
            {
                InitialBalance = 0
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void Backtest_TooManyParameters_ShouldBeRejected()
    {
        var parameters = Enumerable
            .Range(0, 101)
            .ToDictionary(
                x => $"p{x}",
                _ => "1");

        var request =
            ValidBacktest() with
            {
                Parameters = parameters
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void ValidOptimization_ShouldBeAccepted()
    {
        RequestValidation.Validate(
            ValidOptimization());
    }

    [Fact]
    public void Optimization_EmptyRanges_ShouldBeRejected()
    {
        var request =
            ValidOptimization() with
            {
                Ranges = []
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void Optimization_DuplicateRangeNames_ShouldBeRejected()
    {
        var request =
            ValidOptimization() with
            {
                Ranges =
                [
                    new("ProfitDistance", 1, 10, 1),
                    new("profitdistance", 1, 10, 1)
                ]
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void Optimization_ZeroStep_ShouldBeRejected()
    {
        var request =
            ValidOptimization() with
            {
                Ranges =
                [
                    new("ProfitDistance", 1, 10, 0)
                ]
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void WalkForward_WithoutPositiveWindowSizes_ShouldBeRejected()
    {
        var request =
            ValidOptimization() with
            {
                WalkForward = true,
                TrainBars = 0,
                TestBars = 100,
                StepBars = 100
            };

        Assert.Throws<ApiValidationException>(
            () => RequestValidation.Validate(request));
    }

    [Fact]
    public void ValidReplay_ShouldBeAccepted()
    {
        RequestValidation.Validate(
            new CreateReplayRequest(
                "Timeline replay",
                ReplayMode.Timeline));
    }

    [Fact]
    public void Replay_WithoutName_ShouldBeRejected()
    {
        Assert.Throws<ApiValidationException>(
            () =>
                RequestValidation.Validate(
                    new CreateReplayRequest(
                        "",
                        ReplayMode.Timeline)));
    }

    [Fact]
    public void Replay_ReversedGlobalPositions_ShouldBeRejected()
    {
        Assert.Throws<ApiValidationException>(
            () =>
                RequestValidation.Validate(
                    new CreateReplayRequest(
                        "Replay",
                        ReplayMode.Timeline,
                        FromGlobalPosition: 100,
                        ToGlobalPosition: 10)));
    }

    [Fact]
    public void Replay_ReversedTimeRange_ShouldBeRejected()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<ApiValidationException>(
            () =>
                RequestValidation.Validate(
                    new CreateReplayRequest(
                        "Replay",
                        ReplayMode.Timeline,
                        FromUtc: now,
                        ToUtc: now.AddHours(-1))));
    }

    [Fact]
    public void StrategyComparison_WithoutCandidate_ShouldBeRejected()
    {
        Assert.Throws<ApiValidationException>(
            () =>
                RequestValidation.Validate(
                    new CreateReplayRequest(
                        "Compare",
                        ReplayMode.StrategyComparison)));
    }

    private static UpdateBotConfigurationRequest
        ValidBotConfiguration() =>
        new(
            1,
            "Grid",
            "BTCUSDC",
            TradingEnvironment.Demo,
            "Internal",
            true,
            true,
            0.001m,
            10,
            100m,
            50m,
            2,
            60,
            "Test");

    private static BacktestRequest
        ValidBacktest() =>
        new(
            "BOT8012",
            "BTCUSDC",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            10_000m,
            "Internal",
            0.04m,
            0.01m,
            new Dictionary<string, string>());

    private static OptimizationRequest
        ValidOptimization() =>
        new(
            "BOT8012",
            "BTCUSDC",
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow,
            10_000m,
            "Internal",
            [new OptimizationRangeDto(
                "ProfitDistance",
                100m,
                300m,
                100m)],
            10,
            false,
            null,
            null,
            null);
}
