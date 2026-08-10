using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Enums;
using Xunit;

namespace TradingSystem.Application.Tests;

public sealed class StrategyMetadataTests
{
    [Fact]
    public void EffectivePluginId_WhenPluginIdMissing_ReturnsName()
    {
        var metadata = new StrategyMetadata("BOT8012", "1.0.0", PositionMode.TpOnly, ["BTCUSDC"]);
        Assert.Equal("BOT8012", metadata.EffectivePluginId);
    }

    [Fact]
    public void EffectivePluginId_WhenPluginIdProvided_ReturnsPluginId()
    {
        var metadata = new StrategyMetadata("BOT8012", "1.0.0", PositionMode.TpOnly, ["BTCUSDC"], "grid.v2");
        Assert.Equal("grid.v2", metadata.EffectivePluginId);
    }
}
