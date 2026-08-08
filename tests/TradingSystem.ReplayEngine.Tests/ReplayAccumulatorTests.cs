using TradingSystem.EventStore.Constants;
using TradingSystem.EventStore.Models;
using TradingSystem.ReplayEngine.Accumulator;
using TradingSystem.ReplayEngine.Clock;
using Xunit;

namespace TradingSystem.ReplayEngine.Tests;

public sealed class ReplayAccumulatorTests
{
    [Fact]
    public void Apply_SameEventsInSameOrder_ProducesSameDeterministicHash()
    {
        var events = new[]
        {
            Create(1, TradingEventTypes.SignalReceived, "{}"),
            Create(2, TradingEventTypes.RiskDecisionTaken, "{\"allowed\":true}"),
            Create(3, TradingEventTypes.ExecutionCompleted, "{}")
        };
        var left = new ReplayAccumulator();
        var right = new ReplayAccumulator();
        foreach (var item in events) { left.Apply(item); right.Apply(item); }
        Assert.Equal(left.HashSeed, right.HashSeed);
        Assert.Equal(1, left.Signals);
        Assert.Equal(1, left.RiskAllowed);
        Assert.Equal(1, left.ExecutionsCompleted);
    }

    [Fact]
    public void VirtualClock_DoesNotAllowTimeToMoveBackwards()
    {
        var clock = new ReplayVirtualClock();
        clock.AdvanceTo(new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc));
        clock.AdvanceTo(new DateTime(2026, 7, 20, 9, 59, 59, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc), clock.UtcNow);
    }

    private static StoredTradingEvent Create(long position, string type, string payload)
    {
        var id = Guid.Parse($"00000000-0000-0000-0000-{position:D12}");
        return new StoredTradingEvent(position, new EventEnvelope(id, type, 1, "Signal", "s1", position,
            new DateTime(2026, 7, 20, 10, 0, (int)position, DateTimeKind.Utc), DateTime.UtcNow,
            "BOT8012", "BTCUSDC", null, "s1", "s1", null, "test", payload, "{}"));
    }
}
