using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History.Models;
using TradingSystem.Observability.Pipeline;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Configuration;
using TradingSystem.Signals.Contracts;
using TradingSystem.Signals.Models;
using TradingSystem.Signals.Models.Enums;
using TradingSystem.Signals.Services;

namespace TradingSystem.Signals.Tests;

public sealed class SignalGenerationCoordinatorTests
{
    [Fact]
    public async Task ProcessAsync_TradingViewOnly_DoesNotInvokeGenerator()
    {
        var generator = new FakeGenerator();
        var sut = CreateSut(generator, Mode(SignalGenerationMode.TradingViewOnly));

        await sut.ProcessAsync(Snapshot(), default);

        Assert.Equal(0, generator.Calls);
    }

    [Fact]
    public async Task ProcessAsync_WrongConfiguredInterval_DoesNotInvokeGenerator()
    {
        var generator = new FakeGenerator();
        var options = Mode(SignalGenerationMode.InternalLive);
        options.Bots["BOT8012"].Interval = "30m";
        var sut = CreateSut(generator, options);

        await sut.ProcessAsync(Snapshot(), default);

        Assert.Equal(0, generator.Calls);
    }

    [Fact]
    public async Task ProcessAsync_InternalShadow_RecordsButDoesNotPublish()
    {
        var generator = new FakeGenerator();
        var publisher = new CapturingPublisher();
        var history = new CapturingHistory();
        var sut = CreateSut(generator, Mode(SignalGenerationMode.InternalShadow), publisher: publisher, history: history);

        await sut.ProcessAsync(Snapshot(), default);

        Assert.Equal(1, generator.Calls);
        Assert.Single(history.Signals);
        Assert.Empty(publisher.Signals);
        Assert.Equal("Paper", history.Signals[0].Environment);
    }

    [Fact]
    public async Task ProcessAsync_InternalLive_PublishesWithDeterministicSignalId()
    {
        var generator = new FakeGenerator();
        var publisher = new CapturingPublisher();
        var sut = CreateSut(generator, Mode(SignalGenerationMode.InternalLive), publisher: publisher);

        await sut.ProcessAsync(Snapshot(), default);

        var signal = Assert.Single(publisher.Signals);
        Assert.NotEqual(FakeGenerator.RandomGeneratorSignalId, signal.SignalId);
        Assert.Equal(32, signal.SignalId.Length);
        Assert.All(signal.SignalId, c => Assert.True(char.IsDigit(c) || c is >= 'a' and <= 'f'));
    }

    [Fact]
    public async Task ProcessAsync_SameLogicalSignalAcrossCoordinatorInstances_ProducesSameId()
    {
        var firstPublisher = new CapturingPublisher();
        var secondPublisher = new CapturingPublisher();

        await CreateSut(new FakeGenerator(), Mode(SignalGenerationMode.InternalLive), publisher: firstPublisher)
            .ProcessAsync(Snapshot(), default);

        await CreateSut(new FakeGenerator(), Mode(SignalGenerationMode.InternalLive), publisher: secondPublisher)
            .ProcessAsync(Snapshot(), default);

        Assert.Equal(
            Assert.Single(firstPublisher.Signals).SignalId,
            Assert.Single(secondPublisher.Signals).SignalId);
    }

    [Fact]
    public async Task ProcessAsync_WhenThrottleRejects_DoesNotRecordOrPublish()
    {
        var publisher = new CapturingPublisher();
        var history = new CapturingHistory();
        var throttle = new FixedThrottle(false);
        var sut = CreateSut(new FakeGenerator(), Mode(SignalGenerationMode.InternalLive), publisher, throttle, history);

        await sut.ProcessAsync(Snapshot(), default);

        Assert.Empty(history.Signals);
        Assert.Empty(publisher.Signals);
        Assert.Equal(1, throttle.Calls);
    }

    [Fact]
    public async Task ProcessAsync_GeneratorProducesWrongSymbol_Throws()
    {
        var generator = new FakeGenerator(signalFactory: x => Signal(x) with { Symbol = "ETHUSDC" });
        var sut = CreateSut(generator, Mode(SignalGenerationMode.InternalLive));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ProcessAsync(Snapshot(), default));

        Assert.Contains("produced signal for symbol", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_GeneratorProducesNonPositivePrice_Throws()
    {
        var generator = new FakeGenerator(signalFactory: x => Signal(x) with { Price = 0m });
        var sut = CreateSut(generator, Mode(SignalGenerationMode.InternalLive));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ProcessAsync(Snapshot(), default));

        Assert.Contains("non-positive signal price", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_ThrottleReceivesConfiguredMinimumInterval()
    {
        var throttle = new FixedThrottle(true);
        var options = Mode(SignalGenerationMode.InternalShadow);
        options.Bots["BOT8012"].MinimumSecondsBetweenGeneratedSignals = 123;
        var sut = CreateSut(new FakeGenerator(), options, throttle: throttle);

        await sut.ProcessAsync(Snapshot(), default);

        Assert.Equal(TimeSpan.FromSeconds(123), throttle.LastMinimumInterval);
        Assert.Equal("LONG", throttle.LastSide);
        Assert.Equal("BOT8012", throttle.LastBot);
        Assert.Equal("BTCUSDC", throttle.LastSymbol);
    }

    private static SignalGenerationCoordinator CreateSut(
        ITradingSignalGenerator generator,
        SignalGenerationOptions options,
        CapturingPublisher? publisher = null,
        FixedThrottle? throttle = null,
        CapturingHistory? history = null)
        => new(
            [generator],
            publisher ?? new CapturingPublisher(),
            throttle ?? new FixedThrottle(true),
            history ?? new CapturingHistory(),
            new TestEnvironment(),
            Options.Create(options),
            NullLogger<SignalGenerationCoordinator>.Instance);

    private static SignalGenerationOptions Mode(SignalGenerationMode mode)
        => new()
        {
            Bots = new Dictionary<string, BotSignalModeOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["BOT8012"] = new()
                {
                    Enabled = true,
                    Mode = mode,
                    Symbol = "BTCUSDC",
                    Interval = "5m",
                    MinimumSecondsBetweenGeneratedSignals = 60
                }
            }
        };

    private static MarketIndicatorSnapshot Snapshot()
    {
        var open = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var close = open.AddMinutes(5);

        return new(
            "BTCUSDC",
            "5m",
            open,
            close,
            60_000m,
            60_300m,
            59_900m,
            60_250m,
            25m,
            new Dictionary<string, decimal>
            {
                ["alligator_jaw"] = 60_000m,
                ["alligator_teeth"] = 60_080m,
                ["alligator_lips"] = 60_150m,
                ["sma200"] = 59_700m
            });
    }

    private static GeneratedTradingSignal Signal(MarketIndicatorSnapshot x)
        => new(
            FakeGenerator.RandomGeneratorSignalId,
            "BOT8012",
            "1.0.0",
            x.Symbol,
            "LONG",
            "internal-indicators",
            x.CandleCloseTimeUtc,
            x.CandleOpenTimeUtc,
            x.Interval,
            x.Close,
            "test",
            new Dictionary<string, object?>());

    private sealed class FakeGenerator(
        Func<MarketIndicatorSnapshot, GeneratedTradingSignal?>? signalFactory = null)
        : ITradingSignalGenerator
    {
        public const string RandomGeneratorSignalId = "random-generator-id";

        public int Calls { get; private set; }
        public string BotName => "BOT8012";
        public string StrategyVersion => "1.0.0";
        public IReadOnlyCollection<string> SupportedSymbols => ["BTCUSDC"];

        public ValueTask<GeneratedTradingSignal?> GenerateAsync(MarketIndicatorSnapshot snapshot, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
#pragma warning disable CS8619
            return ValueTask.FromResult(signalFactory?.Invoke(snapshot) ?? Signal(snapshot));
#pragma warning restore CS8619
        }
    }

    private sealed class CapturingPublisher : ISignalPublisher
    {
        public List<GeneratedTradingSignal> Signals { get; } = [];

        public Task PublishAsync(GeneratedTradingSignal signal, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Signals.Add(signal);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedThrottle(bool acquired) : IDistributedSignalThrottleStore
    {
        public int Calls { get; private set; }
        public string? LastBot { get; private set; }
        public string? LastSymbol { get; private set; }
        public string? LastSide { get; private set; }
        public TimeSpan? LastMinimumInterval { get; private set; }

        public Task<bool> TryAcquireAsync(
            string botName,
            string symbol,
            string side,
            DateTime signalTimeUtc,
            TimeSpan minimumInterval,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            LastBot = botName;
            LastSymbol = symbol;
            LastSide = side;
            LastMinimumInterval = minimumInterval;
            return Task.FromResult(acquired);
        }
    }

    private sealed class CapturingHistory : ITradingPipelineRecorder
    {
        public List<SignalHistoryRecord> Signals { get; } = [];

        public Task RecordSignalAsync(SignalHistoryRecord record, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Signals.Add(record);
            return Task.CompletedTask;
        }

        public Task RecordDecisionAsync(DecisionHistoryRecord record, CancellationToken ct)
            => Task.CompletedTask;

        public Task UpsertPositionAsync(PositionHistoryRecord record, CancellationToken ct)
            => Task.CompletedTask;

        public Task RecordPositionEventAsync(PositionEventHistoryRecord record, CancellationToken ct)
            => Task.CompletedTask;

        public Task RecordOrderEventAsync(OrderEventHistoryRecord record, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class TestEnvironment : ITradingEnvironmentProvider
    {
        public string EnvironmentName => "Paper";
    }
}
