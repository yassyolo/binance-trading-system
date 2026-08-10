using TradingSystem.Dashboard.Application.Models;
using TradingSystem.Dashboard.Contracts.Models.Backtesting;
using TradingSystem.Dashboard.Contracts.Models.Bots;
using TradingSystem.Dashboard.Contracts.Models.Enums;
using Xunit;

namespace TradingSystem.Dashboard.Application.Tests;

public sealed class DashboardValidationTests
{
    [Fact]
    public void Configuration_ValidRequest_DoesNotThrow()
    {
        DashboardValidation.Validate(ValidConfiguration());
    }

    [Fact]
    public void Configuration_InvalidVersion_Throws()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { ExpectedVersion = 0 }));
    }

    [Fact]
    public void Configuration_EmptyStrategyType_Throws()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { StrategyType = "" }));
    }

    [Fact]
    public void Configuration_EmptySymbol_Throws()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { Symbol = "" }));
    }

    [Fact]
    public void Configuration_NonPositiveQuantity_Throws()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { Quantity = 0 }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(126)]
    public void Configuration_InvalidLeverage_Throws(int leverage)
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { Leverage = leverage }));
    }

    [Fact]
    public void Configuration_NegativeCooldown_Throws()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { CooldownSeconds = -1 }));
    }

    [Fact]
    public void Configuration_NonPositiveOptionalDistances_Throw()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { PriceDistance = 0 }));
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { ProfitDistance = 0 }));
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { OrderSideLimit = 0 }));
    }

    [Fact]
    public void Configuration_NullOptionalDistances_AreAllowed()
    {
        DashboardValidation.Validate(ValidConfiguration() with { PriceDistance = null, ProfitDistance = null, OrderSideLimit = null });
    }

    [Fact]
    public void Configuration_EmptyReason_Throws()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidConfiguration() with { Reason = "" }));
    }

    [Fact]
    public void Command_NormalCommandWithoutConfirmation_IsAllowed()
    {
        DashboardValidation.Validate(new BotCommandRequest(BotCommandType.Start, "start", false));
    }

    [Theory]
    [InlineData(BotCommandType.EmergencyStop)]
    [InlineData(BotCommandType.ClosePosition)]
    [InlineData(BotCommandType.CancelTakeProfit)]
    [InlineData(BotCommandType.RecreateTakeProfit)]
    public void Command_DangerousCommandWithoutConfirmation_Throws(BotCommandType command)
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(new BotCommandRequest(command, "reason", false, PositionId: "p1")));
    }

    [Theory]
    [InlineData(BotCommandType.ClosePosition)]
    [InlineData(BotCommandType.CancelTakeProfit)]
    [InlineData(BotCommandType.RecreateTakeProfit)]
    public void Command_PositionCommandWithoutPositionId_Throws(BotCommandType command)
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(new BotCommandRequest(command, "reason", true)));
    }

    [Fact]
    public void Command_EmptyReason_Throws()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(new BotCommandRequest(BotCommandType.Start, "", false)));
    }

    [Fact]
    public void Backtest_ValidRequest_DoesNotThrow()
    {
        DashboardValidation.Validate(ValidBacktest());
    }

    [Fact]
    public void Backtest_InvalidPeriod_Throws()
    {
        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidBacktest() with { FromUtc = now, ToUtc = now }));
    }

    [Fact]
    public void Backtest_NonPositiveBalance_Throws()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidBacktest() with { InitialBalance = 0 }));
    }

    [Fact]
    public void Backtest_NegativeCosts_Throw()
    {
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidBacktest() with { CommissionPercent = -1 }));
        Assert.Throws<ArgumentException>(() => DashboardValidation.Validate(ValidBacktest() with { SlippagePercent = -1 }));
    }

    private static UpdateBotConfigurationRequest ValidConfiguration() => new(1, "Grid", "BTCUSDC", TradingEnvironment.Demo, "Internal", true, true, 0.001m, 10, 100m, 50m, 2, 60, "test");
    private static BacktestRequest ValidBacktest() => new("BOT8012", "BTCUSDC", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 10_000m, "Internal", 0.04m, 0.01m, new Dictionary<string, string>());
}
