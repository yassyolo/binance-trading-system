using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Enums;
using TradingSystem.StrategyPlugins.Catalog;
using Xunit;

namespace TradingSystem.StrategyPlugins.Tests;

public sealed class StrategyPluginCatalogTests
{
    [Fact]
    public void Catalog_UsesPluginIdAndMetadata()
    {
        var strategy = new FakeStrategy();
        var catalog = new StrategyPluginCatalog([strategy], []);
        var plugin = catalog.GetRequired("sample.grid");
        Assert.Equal("Sample Grid", plugin.DisplayName);
        Assert.Equal("2.1.0", plugin.Version);
    }

    private sealed class FakeStrategy : ITradingStrategy
    {
        public StrategyMetadata Metadata { get; } = new("BOTTEST", "2.1.0", PositionMode.TpOnly, ["BTCUSDC"], "sample.grid", "Sample Grid", "Test strategy");
        public Task<StrategyDecision> DecideAsync(StrategyContext context, CancellationToken cancellationToken)
            => Task.FromResult(StrategyDecision.Block(PositionSide.Long, "test"));
    }
}
