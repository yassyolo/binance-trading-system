using StrategyService.Bots.Bot8011;
using StrategyService.Bots.Bot8015;
using StrategyService.Bots.Bot8016;
using StrategyService.Bots.Bot8016.Models;
using TradingSystem.Application.Time;
using Xunit;

namespace StrategyService.Tests;

public sealed class BotStateHelperTests
{
    [Fact]
    public void Bot8011TrailingPriceCache_BeforeSet_ReturnsFalse()
    {
        var sut = new Bot8011TrailingPriceCache(new TestClock(DateTime.UtcNow));
        Assert.False(sut.TryGetFresh(TimeSpan.FromSeconds(10), out var price));
        Assert.Equal(0m, price);
    }

    [Fact]
    public void Bot8011TrailingPriceCache_AfterSetWithinAge_ReturnsValue()
    {
        var now = DateTime.UtcNow;
        var clock = new MutableClock(now);
        var sut = new Bot8011TrailingPriceCache(clock);

        sut.Set(100m);
        clock.UtcNow = now.AddSeconds(5);

        Assert.True(sut.TryGetFresh(TimeSpan.FromSeconds(10), out var price));
        Assert.Equal(100m, price);
    }

    [Fact]
    public void Bot8011TrailingPriceCache_AfterAgeExpires_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var clock = new MutableClock(now);
        var sut = new Bot8011TrailingPriceCache(clock);

        sut.Set(100m);
        clock.UtcNow = now.AddSeconds(11);

        Assert.False(sut.TryGetFresh(TimeSpan.FromSeconds(10), out _));
    }

    [Fact]
    public void Bot8015TrailingPriceCache_AfterSetWithinAge_ReturnsValue()
    {
        var now = DateTime.UtcNow;
        var clock = new MutableClock(now);
        var sut = new Bot8015TrailingPriceCache(clock);

        sut.Set(99m);

        Assert.True(sut.TryGetFresh(TimeSpan.FromSeconds(1), out var price));
        Assert.Equal(99m, price);
    }

    [Fact]
    public void Bot8016MarketState_StoresIndicator()
    {
        var sut = new Bot8016MarketState();
        var indicator = new Bot8016IndicatorSnapshot("BTCUSDC", "5m", 1, 2, 3m, 4m, 5m, 6m);

        sut.UpdateIndicator(indicator);

        Assert.Same(indicator, sut.GetIndicator());
    }

    [Fact]
    public void Bot8016MarketState_StoresLatestCandlePerInterval()
    {
        var sut = new Bot8016MarketState();
        var first = new Bot8016Candle("BTCUSDC", "1m", 1, 2, 1m, 2m, 0m, 1m, true);
        var second = first with { CloseTime = 3, Close = 2m };

        sut.UpdateCandle(first);
        sut.UpdateCandle(second);

        Assert.Equal(second, sut.GetLatestCandle("1M"));
        Assert.Null(sut.GetLatestCandle("5m"));
    }

    private sealed class TestClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class MutableClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }
}
