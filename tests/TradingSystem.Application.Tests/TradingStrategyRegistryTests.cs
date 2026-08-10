using TradingSystem.Application.Strategies;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Enums;
using Xunit;

namespace TradingSystem.Application.Tests;

public sealed class TradingStrategyRegistryTests
{
    [Fact]
    public void GetRequired_ByName_ReturnsStrategy()
    {
        var strategy = new FakeStrategy("BOT1", "plugin.one");
        var sut = new TradingStrategyRegistry([strategy]);

        Assert.Same(strategy, sut.GetRequired("bot1"));
    }

    [Fact]
    public void GetRequired_ByPluginId_ReturnsStrategy()
    {
        var strategy = new FakeStrategy("BOT1", "plugin.one");
        var sut = new TradingStrategyRegistry([strategy]);

        Assert.Same(strategy, sut.GetRequired("PLUGIN.ONE"));
    }

    [Fact]
    public void GetRequired_WhenMissing_Throws()
    {
        var sut = new TradingStrategyRegistry([]);
        Assert.Throws<InvalidOperationException>(() => sut.GetRequired("missing"));
    }

    [Fact]
    public void Constructor_DuplicateNameForDifferentStrategies_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new TradingStrategyRegistry([new FakeStrategy("BOT1", "p1"), new FakeStrategy("BOT1", "p2")]));
    }

    [Fact]
    public void Constructor_DuplicatePluginIdForDifferentStrategies_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new TradingStrategyRegistry([new FakeStrategy("BOT1", "same"), new FakeStrategy("BOT2", "same")]));
    }

    [Fact]
    public void GetAllMetadata_ReturnsEachStrategyOnce()
    {
        var first = new FakeStrategy("BOT1", "plugin.one");
        var second = new FakeStrategy("BOT2", "plugin.two");
        var sut = new TradingStrategyRegistry([first, second]);

        var result = sut.GetAllMetadata();

        Assert.Equal(2, result.Count);
    }

    private sealed class FakeStrategy(string name, string pluginId) : ITradingStrategy
    {
        public StrategyMetadata Metadata { get; } = new(name, "1.0", PositionMode.TpOnly, ["BTCUSDC"], pluginId);
        public Task<StrategyDecision> DecideAsync(StrategyContext context, CancellationToken ct) => Task.FromResult(StrategyDecision.Block(PositionSide.Long, "test"));
    }
}
