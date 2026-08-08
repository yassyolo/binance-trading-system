using StrategyService.Bots.Bot8011.Configuration;
using StrategyService.Bots.Bot8012.Configuration;
using StrategyService.Bots.Bot8013.Configuration;
using StrategyService.Bots.Bot8014.Configuration;
using StrategyService.Bots.Bot8015.Configuration;
using StrategyService.Bots.Bot8016.Configuration;
using StrategyService.Runtime.Configuration;
using StrategyService.Services.Configuration;
using Xunit;

namespace StrategyService.Tests;

public sealed class OptionsValidatorTests
{
    [Fact]
    public void Bot8011_DefaultOptions_Succeed()
    {
        Assert.True(new Bot8011OptionsValidator().Validate(null, new Bot8011Options()).Succeeded);
    }

    [Fact]
    public void Bot8011_InvalidCoreValues_Fail()
    {
        var options = new Bot8011Options { BotName = "", Symbol = "", Quantity = 0, Leverage = 126, InitialStopLossDistance = 0, TakeProfitPercent = 0, OrderSideLimit = 0, Stop3TrailingStep = 0, Stop3TrailingBuffer = 0, TrailingFallbackIntervalSeconds = 0 };
        Assert.False(new Bot8011OptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Bot8012_DefaultOptions_Succeed()
    {
        Assert.True(new Bot8012OptionsValidator().Validate(null, new Bot8012Options()).Succeeded);
    }

    [Fact]
    public void Bot8012_InvalidCoreValues_Fail()
    {
        var options = new Bot8012Options { BotName = "", Symbol = "", Quantity = 0, Leverage = 0, ProfitDistance = 0, PriceDistance = 0, OrderSideLimit = 0, CooldownSeconds = -1 };
        Assert.False(new Bot8012OptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Bot8013_DefaultOptions_Succeed()
    {
        Assert.True(new Bot8013OptionsValidator().Validate(null, new Bot8013Options()).Succeeded);
    }

    [Fact]
    public void Bot8013_InvalidCoreValues_Fail()
    {
        var options = new Bot8013Options { BotName = "", Symbol = "", Quantity = 0, Leverage = 126, PriceDistance = 0, ProfitDistance = 0, OrderSideLimit = 0, CooldownSeconds = -1 };
        Assert.False(new Bot8013OptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Bot8014_DefaultOptions_Succeed()
    {
        Assert.True(new Bot8014OptionsValidator().Validate(null, new Bot8014Options()).Succeeded);
    }

    [Fact]
    public void Bot8014_InvalidCoreValues_Fail()
    {
        var options = new Bot8014Options { BotName = "", Symbol = "", Quantity = 0, Leverage = 0, PriceDistance = 0, ProfitDistance = 0, OrderSideLimit = 0, CooldownSeconds = -1 };
        Assert.False(new Bot8014OptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Bot8015_DefaultOptions_Succeed()
    {
        Assert.True(new Bot8015OptionsValidator().Validate(null, new Bot8015Options()).Succeeded);
    }

    [Fact]
    public void Bot8015_InvalidCoreValues_Fail()
    {
        var options = new Bot8015Options { BotName = "", Symbol = "", Quantity = 0, InitialStopLoss = 0, TpPercent = 0, Leverage = 126, OrderSideLimit = 0, Stop3TrailingStep = 0, Stop3TrailingBuffer = 0 };
        Assert.False(new Bot8015OptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Bot8016_DefaultOptions_Succeed()
    {
        Assert.True(new Bot8016OptionsValidator().Validate(null, new Bot8016Options()).Succeeded);
    }

    [Fact]
    public void Bot8016_InvalidCoreValues_Fail()
    {
        var options = new Bot8016Options { BotName = "", Symbol = "", Quantity = 0, Leverage = 126, InitialStopLossFallback = 0, TpPercent = 0, Stop3EntryOffset = -1, PositionSideLimit = 0, MinimumSignalCandleRange = -1, AlligatorMaxAgeSeconds = 0, HealingIntervalSeconds = 0, EntryTimeframe = "", ExitTimeframe = "", AlligatorChannel = "" };
        Assert.False(new Bot8016OptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void BotRuntime_DefaultOptions_Succeed()
    {
        Assert.True(new BotRuntimeOptionsValidator().Validate(null, new BotRuntimeOptions()).Succeeded);
    }

    [Fact]
    public void BotRuntime_InvalidValues_Fail()
    {
        var options = new BotRuntimeOptions { CommandPollSeconds = 0, CommandBatchSize = 0, CommandProcessingTimeoutSeconds = 9, ConfigurationRefreshSeconds = 0, MaximumCommandAttempts = 0 };
        Assert.False(new BotRuntimeOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Telegram_DisabledWithoutCredentials_Succeeds()
    {
        Assert.True(new TelegramOptionsValidator().Validate(null, new TelegramOptions { Enabled = false }).Succeeded);
    }

    [Fact]
    public void Telegram_EnabledWithoutCredentials_Fails()
    {
        Assert.False(new TelegramOptionsValidator().Validate(null, new TelegramOptions { Enabled = true, BotToken = "", ChatId = "" }).Succeeded);
    }

    [Fact]
    public void Telegram_EnabledWithCredentials_Succeeds()
    {
        Assert.True(new TelegramOptionsValidator().Validate(null, new TelegramOptions { Enabled = true, BotToken = "token", ChatId = "chat" }).Succeeded);
    }
}
