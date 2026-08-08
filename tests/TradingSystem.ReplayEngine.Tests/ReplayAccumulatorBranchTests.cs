using TradingSystem.EventStore.Constants;
using TradingSystem.EventStore.Models;
using TradingSystem.ReplayEngine.Accumulator;
using Xunit;

namespace TradingSystem.ReplayEngine.Tests;

public sealed class ReplayAccumulatorBranchTests
{
    [Fact]
    public void Apply_RiskDenied_IncrementsRiskBlocked()
    {
        var sut = new ReplayAccumulator();

        sut.Apply(
            Create(
                1,
                TradingEventTypes.RiskDecisionTaken,
                """{"allowed":false}"""));

        Assert.Equal(1, sut.ProcessedEvents);
        Assert.Equal(0, sut.RiskAllowed);
        Assert.Equal(1, sut.RiskBlocked);
    }

    [Fact]
    public void Apply_MalformedRiskPayload_IsCountedAsBlocked()
    {
        var sut = new ReplayAccumulator();

        sut.Apply(
            Create(
                1,
                TradingEventTypes.RiskDecisionTaken,
                "{not-json"));

        Assert.Equal(1, sut.RiskBlocked);
    }

    [Fact]
    public void Apply_AllKnownEventTypes_UpdatesExpectedCounters()
    {
        var sut = new ReplayAccumulator();

        sut.Apply(Create(1, TradingEventTypes.SignalReceived, "{}"));
        sut.Apply(Create(2, TradingEventTypes.StrategyDecisionTaken, "{}"));
        sut.Apply(Create(3, TradingEventTypes.RiskDecisionTaken, """{"allowed":true}"""));
        sut.Apply(Create(4, TradingEventTypes.ExecutionCompleted, "{}"));
        sut.Apply(Create(5, TradingEventTypes.ExecutionFailed, "{}"));
        sut.Apply(Create(6, TradingEventTypes.PositionOpened, "{}"));
        sut.Apply(Create(7, TradingEventTypes.PositionClosed, "{}"));

        Assert.Equal(7, sut.ProcessedEvents);
        Assert.Equal(1, sut.Signals);
        Assert.Equal(1, sut.StrategyDecisions);
        Assert.Equal(1, sut.RiskAllowed);
        Assert.Equal(1, sut.ExecutionsCompleted);
        Assert.Equal(1, sut.ExecutionsFailed);
        Assert.Equal(1, sut.PositionsOpened);
        Assert.Equal(1, sut.PositionsClosed);
    }

    [Fact]
    public void Apply_UnknownEvent_StillIncrementsProcessedAndChangesHash()
    {
        var sut = new ReplayAccumulator();
        var before = sut.HashSeed;

        sut.Apply(Create(1, "UnknownEvent", "{}"));

        Assert.Equal(1, sut.ProcessedEvents);
        Assert.NotEqual(before, sut.HashSeed);
        Assert.False(string.IsNullOrWhiteSpace(sut.HashSeed));
    }

    [Fact]
    public void ToSummary_MapsAccumulatorState()
    {
        var sut = new ReplayAccumulator
        {
            FailedEvents = 2,
            CandidateMatches = 3,
            CandidateDifferences = 4
        };

        sut.Apply(Create(1, TradingEventTypes.SignalReceived, "{}"));

        var replayId = Guid.NewGuid();
        var summary = sut.ToSummary(replayId);

        Assert.Equal(replayId, summary.ReplayId);
        Assert.Equal(sut.ProcessedEvents, summary.ProcessedEvents);
        Assert.Equal(sut.FailedEvents, summary.FailedEvents);
        Assert.Equal(sut.Signals, summary.Signals);
        Assert.Equal(sut.CandidateMatches, summary.CandidateMatches);
        Assert.Equal(sut.CandidateDifferences, summary.CandidateDifferences);
    }

    private static StoredTradingEvent Create(
        long position,
        string type,
        string payload)
    {
        var id = Guid.Parse(
            $"00000000-0000-0000-0000-{position:D12}");

        return new StoredTradingEvent(
            position,
            new EventEnvelope(
                id,
                type,
                1,
                "Signal",
                "s1",
                position,
                new DateTime(
                    2026,
                    7,
                    20,
                    10,
                    0,
                    (int)position,
                    DateTimeKind.Utc),
                DateTime.UtcNow,
                "BOT8012",
                "BTCUSDC",
                null,
                "s1",
                "s1",
                null,
                "test",
                payload,
                "{}"));
    }
}
