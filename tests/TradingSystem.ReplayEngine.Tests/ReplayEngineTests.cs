using TradingSystem.EventStore.Constants;
using TradingSystem.EventStore.Models;
using TradingSystem.ReplayEngine.Accumulator;
using TradingSystem.ReplayEngine.Clock;
using TradingSystem.ReplayEngine.Contracts;
using TradingSystem.ReplayEngine.Engine;
using TradingSystem.ReplayEngine.Evaluator;
using TradingSystem.ReplayEngine.Models;
using TradingSystem.ReplayEngine.Models.Enums;
using TradingSystem.ReplayEngine.Store;
using Xunit;

namespace TradingSystem.ReplayEngine.Tests;

public sealed class ReplayEngineTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ReplayAccumulator_EmptySummary_UsesDeterministicSha256()
    {
        var result = new ReplayAccumulator().ToSummary(Guid.NewGuid());

        Assert.Equal(
            "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
            result.DeterministicHash);
    }

    [Fact]
    public void ReplayAccumulator_MalformedRiskPayload_CountsAsBlocked()
    {
        var accumulator = new ReplayAccumulator();

        accumulator.Apply(Event(1, TradingEventTypes.RiskDecisionTaken, "{broken"));

        Assert.Equal(0, accumulator.RiskAllowed);
        Assert.Equal(1, accumulator.RiskBlocked);
    }

    [Fact]
    public void ReplayVirtualClock_UnspecifiedTime_IsTreatedAsUtc()
    {
        var clock = new ReplayVirtualClock();
        var value = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Unspecified);

        clock.AdvanceTo(value);

        Assert.Equal(DateTimeKind.Utc, clock.UtcNow.Kind);
        Assert.Equal(new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc), clock.UtcNow);
    }

    [Fact]
    public async Task RunAsync_OrdersBatchByGlobalPosition_AndPersistsCheckpoint()
    {
        var source = new FakeSource(
        [
            Event(2, TradingEventTypes.ExecutionCompleted, "{}"),
            Event(1, TradingEventTypes.SignalReceived, "{}")
        ]);
        var store = new FakeStore();
        var sut = new ReplayEngine.Engine.ReplayEngine(source, store, []);

        var result = await sut.RunAsync(Job(new CreateReplayRequest("Replay", ReplayMode.Timeline)), null, default);

        Assert.Equal(2, result.ProcessedEvents);
        Assert.Equal(2, store.LastCheckpointPosition);
        Assert.NotNull(store.CompletedSummary);
    }

    [Fact]
    public async Task RunAsync_WhenSourceReturnsNonForwardPosition_Throws()
    {
        var source = new FakeSource([Event(1, TradingEventTypes.SignalReceived, "{}")]);
        var store = new FakeStore();
        var job = Job(new CreateReplayRequest("Replay", ReplayMode.Timeline)) with { LastGlobalPosition = 1 };
        var sut = new ReplayEngine.Engine.ReplayEngine(source, store, []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync(job, null, default));
    }

    [Fact]
    public async Task RunAsync_WhenCancellationRequested_MarksCancelled()
    {
        var store = new FakeStore { CancellationRequested = true };
        var sut = new ReplayEngine.Engine.ReplayEngine(
            new FakeSource([Event(1, TradingEventTypes.SignalReceived, "{}")]),
            store,
            []);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.RunAsync(Job(new CreateReplayRequest("Replay", ReplayMode.Timeline)), null, default));

        Assert.True(store.MarkCancelledCalled);
    }

    [Fact]
    public async Task RunAsync_StrategyComparison_MatchingCandidate_IncrementsMatch()
    {
        var source = new FakeSource(
        [
            Event(1, TradingEventTypes.StrategyDecisionTaken, """{"decision":"Open","reason":"recorded"}""")
        ]);
        var store = new FakeStore();
        var evaluator = new FixedEvaluator("candidate", "1.0.0", new("Open", "candidate", "{}"));
        var request = new CreateReplayRequest(
            "Compare",
            ReplayMode.StrategyComparison,
            CandidateStrategyPluginId: "candidate",
            CandidateStrategyVersion: "1.0.0");
        var sut = new ReplayEngine.Engine.ReplayEngine(source, store, [evaluator]);

        var result = await sut.RunAsync(Job(request), null, default);

        Assert.Equal(1, result.CandidateMatches);
        Assert.Equal(0, result.CandidateDifferences);
    }

    [Fact]
    public async Task RunAsync_StrategyComparison_DifferentCandidate_IncrementsDifference()
    {
        var source = new FakeSource(
        [
            Event(1, TradingEventTypes.StrategyDecisionTaken, """{"decision":"Block","reason":"recorded"}""")
        ]);
        var evaluator = new FixedEvaluator("candidate", "1.0.0", new("Open", "candidate", "{}"));
        var request = new CreateReplayRequest(
            "Compare",
            ReplayMode.StrategyComparison,
            CandidateStrategyPluginId: "candidate",
            CandidateStrategyVersion: "1.0.0");

        var result = await new ReplayEngine.Engine.ReplayEngine(source, new FakeStore(), [evaluator])
            .RunAsync(Job(request), null, default);

        Assert.Equal(0, result.CandidateMatches);
        Assert.Equal(1, result.CandidateDifferences);
    }

    [Fact]
    public async Task RunAsync_StrategyComparison_WhenEvaluatorMissing_Throws()
    {
        var request = new CreateReplayRequest(
            "Compare",
            ReplayMode.StrategyComparison,
            CandidateStrategyPluginId: "missing",
            CandidateStrategyVersion: "1.0.0");

        var sut = new ReplayEngine.Engine.ReplayEngine(new FakeSource([]), new FakeStore(), []);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync(Job(request), null, default));
    }

    [Fact]
    public async Task RunAsync_StopOnErrorFalse_PersistsFailedStepAndContinues()
    {
        var source = new FakeSource(
        [
            Event(1, TradingEventTypes.StrategyDecisionTaken, "{bad-json"),
            Event(2, TradingEventTypes.SignalReceived, "{}")
        ]);
        var evaluator = new RecordedStrategyEvaluator();
        var request = new CreateReplayRequest(
            "Replay",
            ReplayMode.StrategyComparison,
            CandidateStrategyPluginId: evaluator.PluginId,
            CandidateStrategyVersion: evaluator.Version,
            StopOnError: false);
        var store = new FakeStore();

        var result = await new ReplayEngine.Engine.ReplayEngine(source, store, [evaluator])
            .RunAsync(Job(request), null, default);

        Assert.Equal(1, result.FailedEvents);
        Assert.Contains(store.Steps, x => x.GlobalPosition == 1 && !x.Succeeded);
        Assert.Contains(store.Steps, x => x.GlobalPosition == 2 && x.Succeeded);
    }

    private static ReplayJob Job(CreateReplayRequest request)
        => new(
            Guid.NewGuid(),
            request.Name,
            request.Mode,
            ReplayJobStatus.Processing,
            "test",
            request,
            0,
            0,
            0,
            0,
            null,
            null,
            BaseTime,
            BaseTime,
            null,
            null);

    private static StoredTradingEvent Event(long position, string type, string payload)
        => new(
            position,
            new EventEnvelope(
                Guid.Parse($"00000000-0000-0000-0000-{position:D12}"),
                type,
                1,
                "Test",
                "aggregate-1",
                position,
                BaseTime.AddSeconds(position),
                BaseTime.AddSeconds(position),
                "BOT8012",
                "BTCUSDC",
                null,
                "signal-1",
                "corr-1",
                null,
                "test",
                payload,
                "{}"));

    private sealed class FakeSource(IReadOnlyList<StoredTradingEvent> events) : IReplayEventSource
    {
        private bool _read;

        public Task<IReadOnlyList<StoredTradingEvent>> ReadForwardAsync(
            CreateReplayRequest request,
            long afterGlobalPosition,
            int take,
            CancellationToken ct)
        {
            if (_read)
                return Task.FromResult<IReadOnlyList<StoredTradingEvent>>([]);

            _read = true;
            return Task.FromResult(events);
        }

        public Task<long> CountAsync(CreateReplayRequest request, CancellationToken ct)
            => Task.FromResult((long)events.Count);
    }

    private sealed class FakeStore : IReplayJobStore
    {
        public bool CancellationRequested { get; set; }
        public bool MarkCancelledCalled { get; private set; }
        public long LastCheckpointPosition { get; private set; }
        public List<ReplayStepResult> Steps { get; } = [];
        public ReplaySummary? CompletedSummary { get; private set; }

        public Task<Guid> EnqueueAsync(CreateReplayRequest request, string requestedBy, CancellationToken ct)
            => Task.FromResult(Guid.NewGuid());

        public Task<IReadOnlyList<ReplayJob>> ClaimAsync(string workerId, int take, TimeSpan staleAfter, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ReplayJob>>([]);

        public Task<ReplayJob?> GetAsync(Guid replayId, CancellationToken ct)
            => Task.FromResult<ReplayJob?>(null);

        public Task<ReplaySummary?> GetSummaryAsync(Guid replayId, CancellationToken ct)
            => Task.FromResult<ReplaySummary?>(CompletedSummary);

        public Task<IReadOnlyList<ReplayStepResult>> GetStepsAsync(Guid replayId, long afterGlobalPosition, int take, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ReplayStepResult>>(Steps);

        public Task<IReadOnlyList<ReplayJob>> QueryAsync(ReplayJobStatus? status, int skip, int take, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ReplayJob>>([]);

        public Task<ReplayAccumulator?> LoadCheckpointAsync(Guid replayId, CancellationToken ct)
            => Task.FromResult<ReplayAccumulator?>(null);

        public Task SaveCheckpointAsync(
            Guid replayId,
            long globalPosition,
            ReplayAccumulator accumulator,
            int progressPercent,
            string progressStage,
            CancellationToken ct)
        {
            LastCheckpointPosition = globalPosition;
            return Task.CompletedTask;
        }

        public Task SaveStepsAsync(IReadOnlyCollection<ReplayStepResult> steps, CancellationToken ct)
        {
            Steps.AddRange(steps);
            return Task.CompletedTask;
        }

        public Task CompleteAsync(Guid replayId, ReplaySummary summary, CancellationToken ct)
        {
            CompletedSummary = summary;
            return Task.CompletedTask;
        }

        public Task FailAsync(Guid replayId, string error, CancellationToken ct) => Task.CompletedTask;
        public Task CancelAsync(Guid replayId, string actor, CancellationToken ct) => Task.CompletedTask;

        public Task MarkCancelledAsync(Guid replayId, CancellationToken ct)
        {
            MarkCancelledCalled = true;
            return Task.CompletedTask;
        }

        public Task<bool> IsCancellationRequestedAsync(Guid replayId, CancellationToken ct)
            => Task.FromResult(CancellationRequested);
    }

    private sealed class FixedEvaluator(
        string pluginId,
        string version,
        ReplayCandidateDecision decision) : IReplayStrategyEvaluator
    {
        public string PluginId => pluginId;
        public string Version => version;

        public ValueTask<ReplayCandidateDecision?> EvaluateAsync(
            StoredTradingEvent sourceEvent,
            ReplayContext context,
            CancellationToken ct)
            => ValueTask.FromResult<ReplayCandidateDecision?>(decision);
    }
}
