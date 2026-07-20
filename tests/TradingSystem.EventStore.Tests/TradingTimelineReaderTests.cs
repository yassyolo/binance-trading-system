using TradingSystem.EventStore;
using Xunit;

namespace TradingSystem.EventStore.Tests;

public sealed class TradingTimelineReaderTests
{
    [Fact]
    public async Task ReadAsync_MapsStoredEventsInStoreOrder()
    {
        var envelope = new EventEnvelope(Guid.NewGuid(), TradingEventTypes.SignalReceived, 1,
            "Signal", "signal-1", 1, DateTime.UtcNow, DateTime.UtcNow, "BOT8012", "BTCUSDC",
            null, "signal-1", "correlation-1", null, "TradingView", "{}", "{}");
        var reader = new TradingTimelineReader(new FakeStore([new StoredTradingEvent(42, envelope)]));

        var result = await reader.ReadAsync(new EventStoreQuery(SignalId: "signal-1"), default);

        Assert.Single(result);
        Assert.Equal(42, result[0].GlobalPosition);
        Assert.Equal(TradingEventTypes.SignalReceived, result[0].EventType);
    }

    private sealed class FakeStore(IReadOnlyList<StoredTradingEvent> events) : ITradingEventStore
    {
        public Task<StoredTradingEvent> AppendAsync(AppendTradingEvent request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<StoredTradingEvent>> ReadAsync(EventStoreQuery query, CancellationToken cancellationToken) => Task.FromResult(events);
        public Task<IReadOnlyList<StoredTradingEvent>> ReadStreamAsync(string aggregateType, string aggregateId, long afterVersion, int take, CancellationToken cancellationToken) => Task.FromResult(events);
    }
}
