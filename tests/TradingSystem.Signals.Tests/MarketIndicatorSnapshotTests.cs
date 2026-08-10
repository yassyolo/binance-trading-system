using TradingSystem.Signals.Models;
using Xunit;

namespace TradingSystem.Signals.Tests;

public sealed class MarketIndicatorSnapshotTests
{
    [Fact]
    public void TryGet_WhenIndicatorExists_ReturnsValue()
    {
        var snapshot = Snapshot(new Dictionary<string, decimal> { ["sma200"] = 123m });

        Assert.True(snapshot.TryGet("sma200", out var value));
        Assert.Equal(123m, value);
    }

    [Fact]
    public void TryGet_WhenIndicatorMissing_ReturnsFalse()
    {
        var snapshot = Snapshot(new Dictionary<string, decimal>());
        Assert.False(snapshot.TryGet("missing", out _));
    }

    private static MarketIndicatorSnapshot Snapshot(IReadOnlyDictionary<string, decimal> indicators) => new("BTCUSDC", "5m", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow, 100m, 110m, 90m, 105m, 1m, indicators);
}
