using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012;
using StrategyService.Bots.Bot8012.Configuration;
using TradingSystem.Signals.Constants;
using TradingSystem.Signals.Models;
using Xunit;

namespace StrategyService.Tests;

public sealed class Bot8012SignalGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_WhenRulesDisabled_ReturnsNull()
    {
        var sut = Create(new Bot8012Options
        {
            SignalRules = new Bot8012SignalRules { Enabled = false }
        });

        var result = await sut.GenerateAsync(Snapshot(close: 110m), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAsync_Long_WhenConfiguredIndicatorsAlign_ReturnsLong()
    {
        var sut = Create();

        var result = await sut.GenerateAsync(
            Snapshot(
                close: 110m,
                indicators: new Dictionary<string, decimal>
                {
                    [IndicatorKeys.BollingerUpper] = 100m,
                    [IndicatorKeys.BollingerLower] = 80m,
                    [IndicatorKeys.AlligatorLips] = 30m,
                    [IndicatorKeys.AlligatorTeeth] = 20m,
                    [IndicatorKeys.AlligatorJaw] = 10m
                }),
            default);

        Assert.NotNull(result);
        Assert.Equal("LONG", result!.Action);
        Assert.Equal("internal-indicators", result.Source);
        Assert.Equal("BOT8012", result.BotName);
    }

    [Fact]
    public async Task GenerateAsync_Short_WhenConfiguredIndicatorsAlign_ReturnsShort()
    {
        var sut = Create();

        var result = await sut.GenerateAsync(
            Snapshot(
                close: 70m,
                indicators: new Dictionary<string, decimal>
                {
                    [IndicatorKeys.BollingerUpper] = 120m,
                    [IndicatorKeys.BollingerLower] = 80m,
                    [IndicatorKeys.AlligatorLips] = 10m,
                    [IndicatorKeys.AlligatorTeeth] = 20m,
                    [IndicatorKeys.AlligatorJaw] = 30m
                }),
            default);

        Assert.NotNull(result);
        Assert.Equal("SHORT", result!.Action);
    }

    [Fact]
    public async Task GenerateAsync_BollingerEquality_DoesNotTriggerLong()
    {
        var sut = Create();

        var result = await sut.GenerateAsync(
            Snapshot(
                close: 100m,
                indicators: new Dictionary<string, decimal>
                {
                    [IndicatorKeys.BollingerUpper] = 100m,
                    [IndicatorKeys.BollingerLower] = 80m,
                    [IndicatorKeys.AlligatorLips] = 30m,
                    [IndicatorKeys.AlligatorTeeth] = 20m,
                    [IndicatorKeys.AlligatorJaw] = 10m
                }),
            default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAsync_WhenRequiredAlligatorValueMissing_ReturnsNull()
    {
        var sut = Create();

        var result = await sut.GenerateAsync(
            Snapshot(
                close: 110m,
                indicators: new Dictionary<string, decimal>
                {
                    [IndicatorKeys.BollingerUpper] = 100m,
                    [IndicatorKeys.BollingerLower] = 80m,
                    [IndicatorKeys.AlligatorLips] = 30m,
                    [IndicatorKeys.AlligatorTeeth] = 20m
                }),
            default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAsync_LongDisabled_ReturnsNullEvenWhenIndicatorsMatch()
    {
        var options = new Bot8012Options
        {
            EnableLong = false,
            SignalRules = new Bot8012SignalRules
            {
                RequireBollingerBreakout = false,
                RequireAlligatorAlignment = false
            }
        };
        var sut = Create(options);

        var result = await sut.GenerateAsync(Snapshot(close: 100m), default);

        Assert.Null(result);
    }

    private static Bot8012SignalGenerator Create(Bot8012Options? options = null)
        => new(Options.Create(options ?? new Bot8012Options()));

    private static MarketIndicatorSnapshot Snapshot(
        decimal close,
        IReadOnlyDictionary<string, decimal>? indicators = null)
        => new(
            "BTCUSDC",
            "5m",
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc),
            100m,
            111m,
            90m,
            close,
            1m,
            indicators ?? new Dictionary<string, decimal>());
}
