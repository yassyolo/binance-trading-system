using TradingSystem.EventStore.Constants;
using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.Models;
using TradingSystem.EventStore.TradingTimeline;
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
        var reader = new TradingEventStoreReader(new FakeStore([new StoredTradingEvent(42, envelope)]));

        var result = await reader.ReadAsync(new EventStoreQuery(SignalId: "signal-1"), default);

        Assert.Single(result);
        Assert.Equal(42, result[0].GlobalPosition);
        Assert.Equal(TradingEventTypes.SignalReceived, result[0].EventType);
    }

    [Fact]
    public async Task ReadAsync_WhenStoreReturnsEmpty_ReturnsEmpty()
    {
        var store = new CapturingStore([]);
        var sut = new TradingEventStoreReader(store);

        var result = await sut.ReadAsync(
            new EventStoreQuery(BotName: "BOT8012"),
            default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadAsync_PassesOriginalQueryToStore()
    {
        var store = new CapturingStore([]);
        var sut = new TradingEventStoreReader(store);

        var query = new EventStoreQuery(
            BotName: "BOT8012",
            Symbol: "BTCUSDC",
            SignalId: "signal-1");

        await sut.ReadAsync(query, default);

        Assert.Same(query, store.LastQuery);
    }

    private sealed class CapturingStore(
        IReadOnlyList<StoredTradingEvent> events)
        : ITradingEventStore
    {
        public EventStoreQuery? LastQuery { get; private set; }

        public Task<StoredTradingEvent> AppendAsync(
            AppendTradingEvent request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StoredTradingEvent>> ReadAsync(
            EventStoreQuery query,
            CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(events);
        }

        public Task<IReadOnlyList<StoredTradingEvent>> ReadStreamAsync(
            string aggregateType,
            string aggregateId,
            long afterVersion,
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult(events);
    }

    private sealed class FakeStore(IReadOnlyList<StoredTradingEvent> events) : ITradingEventStore
    {
        public Task<StoredTradingEvent> AppendAsync(AppendTradingEvent request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<StoredTradingEvent>> ReadAsync(EventStoreQuery query, CancellationToken cancellationToken) => Task.FromResult(events);
        public Task<IReadOnlyList<StoredTradingEvent>> ReadStreamAsync(string aggregateType, string aggregateId, long afterVersion, int take, CancellationToken cancellationToken) => Task.FromResult(events);
    }
}
