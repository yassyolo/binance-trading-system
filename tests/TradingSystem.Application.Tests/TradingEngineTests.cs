using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Engine.Configuration;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Positions.Models;
using TradingSystem.Application.Risk.Contracts;
using TradingSystem.Application.Risk.Models;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Application.Time;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.BotRuntime.Configuration.Models;
using TradingSystem.BotRuntime.Runtime.Contracts;
using TradingSystem.BotRuntime.Runtime.Models;
using TradingSystem.BotRuntime.Runtime.Models.Enums;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;
using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.Models;
using Xunit;

namespace TradingSystem.Application.Tests;

public sealed class TradingEngineTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ProcessSignalAsync_StaleSignal_IsRejectedBeforeIdempotency()
    {
        var idempotency = new FakeIdempotencyStore();
        var sut = CreateSut(idempotency: idempotency);

        var result = await sut.ProcessSignalAsync(Signal(Now.AddMinutes(-3)), default);

        Assert.False(result.Succeeded);
        Assert.False(result.OpenedPosition);
        Assert.Contains("stale", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, idempotency.TryStartCalls);
    }

    [Fact]
    public async Task ProcessSignalAsync_FutureSignalBeyondSkew_IsRejectedBeforeIdempotency()
    {
        var idempotency = new FakeIdempotencyStore();
        var sut = CreateSut(idempotency: idempotency);

        var result = await sut.ProcessSignalAsync(Signal(Now.AddSeconds(11)), default);

        Assert.False(result.Succeeded);
        Assert.Contains("future", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, idempotency.TryStartCalls);
    }

    [Fact]
    public async Task ProcessSignalAsync_DuplicateSignal_ReturnsDuplicateWithoutTakingLock()
    {
        var idempotency = new FakeIdempotencyStore { TryStartResult = false };
        var locks = new FakeLockProvider();
        var sut = CreateSut(idempotency: idempotency, locks: locks);

        var result = await sut.ProcessSignalAsync(Signal(), default);

        Assert.True(result.Succeeded);
        Assert.True(result.Duplicate);
        Assert.Equal(0, locks.Calls);
    }

    [Fact]
    public async Task ProcessSignalAsync_LockUnavailable_ReleasesIdempotency()
    {
        var idempotency = new FakeIdempotencyStore();
        var locks = new FakeLockProvider { Acquire = false };
        var sut = CreateSut(idempotency: idempotency, locks: locks);

        var result = await sut.ProcessSignalAsync(Signal(), default);

        Assert.False(result.Succeeded);
        Assert.Equal(1, idempotency.ReleaseCalls);
        Assert.Equal(0, idempotency.MarkCompletedCalls);
    }

    [Fact]
    public async Task ProcessSignalAsync_RuntimePaused_MarksSignalCompleted()
    {
        var idempotency = new FakeIdempotencyStore();
        var runtime = new FakeRuntimeStateProvider(
            new BotRuntimeState("BOT8012", BotRuntimeStatus.Paused, 1, Now, "test", "manual pause", true));
        var strategy = new FakeStrategy();
        var sut = CreateSut(idempotency: idempotency, runtimeState: runtime, strategy: strategy);

        var result = await sut.ProcessSignalAsync(Signal(), default);

        Assert.True(result.Succeeded);
        Assert.False(result.OpenedPosition);
        Assert.Contains("BOT_RUNTIME_BLOCKED", result.Reason);
        Assert.Equal(1, idempotency.MarkCompletedCalls);
        Assert.Equal(0, strategy.DecideCalls);
    }

    [Fact]
    public async Task ProcessSignalAsync_ActiveCooldown_BlocksBeforeStrategy()
    {
        var idempotency = new FakeIdempotencyStore();
        var cooldown = new FakeCooldownStore { Remaining = TimeSpan.FromSeconds(90) };
        var strategy = new FakeStrategy();
        var sut = CreateSut(idempotency: idempotency, cooldown: cooldown, strategy: strategy);

        var result = await sut.ProcessSignalAsync(Signal(), default);

        Assert.True(result.Succeeded);
        Assert.False(result.OpenedPosition);
        Assert.Contains("Cooldown active", result.Reason);
        Assert.Equal(1, idempotency.MarkCompletedCalls);
        Assert.Equal(0, strategy.DecideCalls);
    }

    [Fact]
    public async Task ProcessSignalAsync_StrategyBlocks_MarksSignalCompleted()
    {
        var idempotency = new FakeIdempotencyStore();
        var strategy = new FakeStrategy { Decision = StrategyDecision.Block(PositionSide.Long, "spacing blocked") };
        var risk = new FakeRiskManager();
        var sut = CreateSut(idempotency: idempotency, strategy: strategy, risk: risk);

        var result = await sut.ProcessSignalAsync(Signal(), default);

        Assert.True(result.Succeeded);
        Assert.False(result.OpenedPosition);
        Assert.Equal("spacing blocked", result.Reason);
        Assert.Equal(1, idempotency.MarkCompletedCalls);
        Assert.Equal(0, risk.Calls);
    }

    [Fact]
    public async Task ProcessSignalAsync_RiskBlocks_MarksSignalCompletedWithoutExecution()
    {
        var idempotency = new FakeIdempotencyStore();
        var executor = new FakeTradeExecutor();
        var risk = new FakeRiskManager { Decision = RiskDecision.Block("MAX_EXPOSURE", "limit reached") };
        var sut = CreateSut(idempotency: idempotency, executor: executor, risk: risk);

        var result = await sut.ProcessSignalAsync(Signal(), default);

        Assert.True(result.Succeeded);
        Assert.False(result.OpenedPosition);
        Assert.Contains("RISK_BLOCKED [MAX_EXPOSURE]", result.Reason);
        Assert.Equal(1, idempotency.MarkCompletedCalls);
        Assert.Equal(0, executor.OpenCalls);
    }

    [Fact]
    public async Task ProcessSignalAsync_ExecutionFailure_ReleasesIdempotencyAndCompletesRiskAdmissionAsFailed()
    {
        var idempotency = new FakeIdempotencyStore();
        var executor = new FakeTradeExecutor { OpenResult = TradeExecutionResult.Failure("exchange failed") };
        var lifecycle = new FakeRiskAdmissionLifecycle();
        var sut = CreateSut(idempotency: idempotency, executor: executor, lifecycle: lifecycle);

        var result = await sut.ProcessSignalAsync(Signal(), default);

        Assert.False(result.Succeeded);
        Assert.False(result.OpenedPosition);
        Assert.Equal(1, idempotency.ReleaseCalls);
        Assert.Equal(1, lifecycle.Calls);
        Assert.False(lifecycle.LastExecutionSucceeded);
    }

    [Fact]
    public async Task ProcessSignalAsync_ExecutionSuccess_MarksCompletedAndPersistsCooldown()
    {
        var idempotency = new FakeIdempotencyStore();
        var cooldown = new FakeCooldownStore();
        var lifecycle = new FakeRiskAdmissionLifecycle();
        var executor = new FakeTradeExecutor { OpenResult = TradeExecutionResult.Success("p1", "opened") };
        var strategy = new FakeStrategyWithCooldown(TimeSpan.FromSeconds(180));
        var sut = CreateSut(
            idempotency: idempotency,
            cooldown: cooldown,
            lifecycle: lifecycle,
            executor: executor,
            strategy: strategy);

        var result = await sut.ProcessSignalAsync(Signal(), default);

        Assert.True(result.Succeeded);
        Assert.True(result.OpenedPosition);
        Assert.Equal("p1", result.ShortId);
        Assert.Equal(1, idempotency.MarkCompletedCalls);
        Assert.Equal(Now.AddSeconds(180), cooldown.LastExpiresAtUtc);
        Assert.Equal(1, lifecycle.Calls);
        Assert.True(lifecycle.LastExecutionSucceeded);
    }

    private static TradingEngine CreateSut(
        FakeIdempotencyStore? idempotency = null,
        FakeLockProvider? locks = null,
        FakeCooldownStore? cooldown = null,
        ITradingStrategy? strategy = null,
        FakeRiskManager? risk = null,
        FakeRiskAdmissionLifecycle? lifecycle = null,
        FakeTradeExecutor? executor = null,
        FakeRuntimeStateProvider? runtimeState = null,
        FakeRuntimeConfigurationProvider? runtimeConfig = null)
    {
        strategy ??= new FakeStrategy();

        return new TradingEngine(
            new FakeStrategyResolver(strategy),
            new FakeMarketPriceProvider(),
            new FakeActivePositionProvider(),
            executor ?? new FakeTradeExecutor(),
            locks ?? new FakeLockProvider(),
            idempotency ?? new FakeIdempotencyStore(),
            cooldown ?? new FakeCooldownStore(),
            new FakeNotifier(),
            risk ?? new FakeRiskManager(),
            lifecycle ?? new FakeRiskAdmissionLifecycle(),
            runtimeState ?? RunningRuntime(),
            runtimeConfig ?? new FakeRuntimeConfigurationProvider(),
            new TestClock(Now),
            Options.Create(new TradingEngineOptions
            {
                ProcessingIdempotencyTtl = TimeSpan.FromMinutes(5),
                CompletedIdempotencyTtl = TimeSpan.FromHours(24),
                OperationLockTtl = TimeSpan.FromSeconds(30),
                MaximumSignalAge = TimeSpan.FromMinutes(2),
                MaximumFutureClockSkew = TimeSpan.FromSeconds(10),
                PostExecutionCompletionRetryCount = 3,
                PostExecutionCompletionRetryDelay = TimeSpan.Zero
            }),
            NullLogger<TradingEngine>.Instance,
            new FakeEventStore());
    }

    private static TradeSignal Signal(DateTime? generatedAtUtc = null)
        => new()
        {
            SignalId = "signal-1",
            BotName = "BOT8012",
            Symbol = "BTCUSDC",
            Side = PositionSide.Long,
            Source = "test",
            GeneratedAtUtc = generatedAtUtc ?? Now
        };

    private static FakeRuntimeStateProvider RunningRuntime()
        => new(new BotRuntimeState("BOT8012", BotRuntimeStatus.Running, 1, Now, "test", null, true));

    private sealed class TestClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class FakeStrategyResolver(ITradingStrategy strategy) : ITradingStrategyResolver
    {
        public Task<ITradingStrategy> ResolveAsync(string botName, CancellationToken ct)
            => Task.FromResult(strategy);
    }

    private class FakeStrategy : ITradingStrategy
    {
        public int DecideCalls { get; private set; }
        public StrategyDecision Decision { get; set; } = StrategyDecision.Open(PositionSide.Long, "open");
        public StrategyMetadata Metadata { get; } = new("BOT8012", "1.0.0", PositionMode.TpOnly, ["BTCUSDC"]);

        public Task<StrategyDecision> DecideAsync(StrategyContext context, CancellationToken ct)
        {
            DecideCalls++;
            return Task.FromResult(Decision);
        }
    }

    private sealed class FakeStrategyWithCooldown(TimeSpan cooldown) : FakeStrategy, IHasSignalCooldown
    {
        public TimeSpan SignalCooldown { get; } = cooldown;
    }

    private sealed class FakeMarketPriceProvider : IMarketPriceProvider
    {
        public Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken ct)
            => Task.FromResult(60_000m);
    }

    private sealed class FakeActivePositionProvider : IActivePositionProvider
    {
        public Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string botName, string symbol, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<ActivePositionView>>([]);
    }

    private sealed class FakeTradeExecutor : ITradeExecutor
    {
        public int OpenCalls { get; private set; }
        public TradeExecutionResult OpenResult { get; set; } = TradeExecutionResult.Success("p1", "opened");

        public Task<TradeExecutionResult> OpenAsync(string botName, string symbol, PositionSide side, string? source, CancellationToken ct)
        {
            OpenCalls++;
            return Task.FromResult(OpenResult);
        }

        public Task<TradeExecutionResult> CloseAsync(string botName, string shortId, string reason, CancellationToken ct)
            => Task.FromResult(TradeExecutionResult.Success(shortId, "closed"));
    }

    private sealed class FakeLockProvider : ITradingOperationLockProvider
    {
        public int Calls { get; private set; }
        public bool Acquire { get; set; } = true;

        public Task<IAsyncDisposable?> TryAcquireAsync(string botName, string symbol, PositionSide side, TimeSpan ttl, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult<IAsyncDisposable?>(Acquire ? new AsyncLock() : null);
        }

        private sealed class AsyncLock : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class FakeIdempotencyStore : ISignalIdempotencyStore
    {
        public bool TryStartResult { get; set; } = true;
        public int TryStartCalls { get; private set; }
        public int MarkCompletedCalls { get; private set; }
        public int ReleaseCalls { get; private set; }

        public Task<bool> TryStartAsync(string signalId, TimeSpan ttl, CancellationToken ct)
        {
            TryStartCalls++;
            return Task.FromResult(TryStartResult);
        }

        public Task MarkCompletedAsync(string signalId, TimeSpan ttl, CancellationToken ct)
        {
            MarkCompletedCalls++;
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(string signalId, CancellationToken ct)
        {
            ReleaseCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCooldownStore : ISignalCooldownStore
    {
        public TimeSpan? Remaining { get; set; }
        public DateTime? LastExpiresAtUtc { get; private set; }

        public Task<TimeSpan?> GetRemainingAsync(string botName, string symbol, PositionSide side, DateTime nowUtc, CancellationToken ct)
            => Task.FromResult(Remaining);

        public Task SetAsync(string botName, string symbol, PositionSide side, DateTime expiresAtUtc, CancellationToken ct)
        {
            LastExpiresAtUtc = expiresAtUtc;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRiskManager : ICentralRiskManager
    {
        public int Calls { get; private set; }
        public RiskDecision Decision { get; set; } = RiskDecision.Allow();

        public Task<RiskDecision> EvaluateOpenAsync(RiskEvaluationContext context, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Decision);
        }
    }

    private sealed class FakeRiskAdmissionLifecycle : IRiskAdmissionLifecycle
    {
        public int Calls { get; private set; }
        public bool LastExecutionSucceeded { get; private set; }

        public Task CompleteAsync(string signalId, bool executionSucceeded, CancellationToken ct)
        {
            Calls++;
            LastExecutionSucceeded = executionSucceeded;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRuntimeStateProvider(BotRuntimeState state) : IBotRuntimeStateProvider
    {
        public Task<BotRuntimeState> GetRequiredAsync(string botName, CancellationToken ct)
            => Task.FromResult(state);

        public void Invalidate(string botName) { }
    }

    private sealed class FakeRuntimeConfigurationProvider : IBotRuntimeConfigurationProvider
    {
        public Task<BotRuntimeConfiguration?> GetAsync(string botName, CancellationToken ct)
            => Task.FromResult<BotRuntimeConfiguration?>(null);

        public void Set(BotRuntimeConfiguration configuration) { }
    }

    private sealed class FakeNotifier : ITradingEngineNotifier
    {
        public Task DecisionMadeAsync(TradeSignal signal, decimal markPrice, StrategyDecision decision, CancellationToken ct)
            => Task.CompletedTask;

        public Task ExecutionCompletedAsync(TradeSignal signal, TradeExecutionResult result, CancellationToken ct)
            => Task.CompletedTask;

        public Task ProcessingFailedAsync(TradeSignal signal, Exception exception, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeEventStore : ITradingEventStore
    {
        public Task<StoredTradingEvent> AppendAsync(AppendTradingEvent request, CancellationToken ct)
            => Task.FromResult<StoredTradingEvent>(null!);

        public Task<IReadOnlyList<StoredTradingEvent>> ReadAsync(EventStoreQuery query, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<StoredTradingEvent>>([]);

        public Task<IReadOnlyList<StoredTradingEvent>> ReadStreamAsync(string aggregateType, string aggregateId, long afterVersion, int take, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<StoredTradingEvent>>([]);
    }
}
