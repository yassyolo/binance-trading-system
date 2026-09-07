using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.BotRuntime.Configuration.Models;
using TradingSystem.Domain.Enums;
using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.Models;
using TradingSystem.Observability.History.Models;
using TradingSystem.Observability.Pipeline;
using TradingSystem.PaperTrading.Configuration;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.PaperTrading.Executor;
using TradingSystem.PaperTrading.Models;
using TradingSystem.PaperTrading.Models.Enums;
using Xunit;

namespace TradingSystem.PaperTrading.Tests;

public sealed class PaperTradingTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task OpenAsync_WhenDisabled_ReturnsFailureWithoutLoadingMarketPrice()
    {
        var prices = new FakeMarketPriceProvider(100m);
        var sut = CreateExecutor(enabled: false, prices: prices);

        var result = await sut.OpenAsync("BOT8012", "BTCUSDC", PositionSide.Long, "test", default);

        Assert.False(result.Succeeded);
        Assert.Contains("disabled", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, prices.Calls);
    }

    [Fact]
    public async Task OpenAsync_WhenMaximumOpenPositionsReached_ReturnsFailure()
    {
        var store = new FakePaperTradingStore
        {
            OpenPositions =
            [
                Position("p1"),
                Position("p2")
            ]
        };
        var sut = CreateExecutor(store: store, maximumOpenPositions: 2);

        var result = await sut.OpenAsync("BOT8012", "BTCUSDC", PositionSide.Long, "test", default);

        Assert.False(result.Succeeded);
        Assert.Contains("maximum open positions", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, store.CreateCalls);
    }

    [Fact]
    public async Task OpenAsync_WhenRuntimeConfigMissing_ReturnsFailure()
    {
        var sut = CreateExecutor(configProvider: new FakeRuntimeConfigurationProvider(null));

        var result = await sut.OpenAsync("BOT8012", "BTCUSDC", PositionSide.Long, "test", default);

        Assert.False(result.Succeeded);
        Assert.Contains("Runtime config", result.Reason);
    }

    [Fact]
    public async Task OpenAsync_Long_AppliesAdverseSlippageDistanceTpAndFees()
    {
        var store = new FakePaperTradingStore();
        var config = Runtime(quantity: 2m, profitDistance: 10m);
        var context = new FakeSignalContextAccessor(
            new TradingSignalExecutionContext("signal-1", "1.2.3", "internal"));

        var sut = CreateExecutor(
            store: store,
            configProvider: new FakeRuntimeConfigurationProvider(config),
            signalContext: context,
            markPrice: 100m,
            slippagePercent: 1m,
            commissionPercent: 0.5m,
            defaultStopLossPercent: 10m);

        var result = await sut.OpenAsync("BOT8012", "btcusdc", PositionSide.Long, "external", default);

        Assert.True(result.Succeeded);

        var position = Assert.Single(store.Created);
        Assert.Equal("BTCUSDC", position.Symbol);
        Assert.Equal(101m, position.EntryPrice);
        Assert.Equal(111m, position.TakeProfitPrice);
        Assert.Equal(90.9m, position.StopLossPrice);
        Assert.Equal(1.01m, position.EntryFee);
        Assert.Equal("signal-1", position.SignalId);
        Assert.Equal("1.2.3", position.StrategyVersion);
        Assert.Equal("external", position.Source);
        Assert.Equal(PaperPositionStatus.Open, position.Status);
    }

    [Fact]
    public async Task OpenAsync_WhenProfitDistanceMissing_UsesDefaultTakeProfitPercent()
    {
        var store = new FakePaperTradingStore();
        var config = Runtime(quantity: 1m, profitDistance: null);
        var sut = CreateExecutor(
            store: store,
            configProvider: new FakeRuntimeConfigurationProvider(config),
            markPrice: 100m,
            slippagePercent: 0m,
            defaultTakeProfitPercent: 5m);

        var result = await sut.OpenAsync("BOT8012", "BTCUSDC", PositionSide.Short, null, default);

        Assert.True(result.Succeeded);

        var position = Assert.Single(store.Created);
        Assert.Equal(95m, position.TakeProfitPrice);
        Assert.Equal("internal", position.Source);
    }

    [Fact]
    public async Task CloseAtPriceAsync_Long_CalculatesExitSlippageFeesAndNetPnl()
    {
        var position = Position("p1", PositionSide.Long, quantity: 2m, entryPrice: 100m, entryFee: 1m);
        var store = new FakePaperTradingStore { PositionById = position };
        var sut = CreateExecutor(
            store: store,
            slippagePercent: 1m,
            commissionPercent: 0.5m);

        var result = await sut.CloseAtPriceAsync("BOT8012", "p1", 120m, "manual", default);

        Assert.True(result.Succeeded);
        Assert.Equal(118.8m, store.LastExitPrice);
        Assert.Equal(1.188m, store.LastExitFee);
        Assert.Equal(35.412m, store.LastRealizedPnl);
        Assert.Equal("manual", store.LastCloseReason);
    }

    [Fact]
    public async Task CloseAtPriceAsync_WhenAlreadyClosed_ReturnsIdempotentSuccess()
    {
        var position = Position("p1");
        position.Status = PaperPositionStatus.Closed;
        var store = new FakePaperTradingStore { PositionById = position };
        var sut = CreateExecutor(store: store);

        var result = await sut.CloseAtPriceAsync("BOT8012", "p1", 100m, "again", default);

        Assert.True(result.Succeeded);
        Assert.Contains("already closed", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, store.TryCloseCalls);
    }

    [Fact]
    public async Task CloseAtPriceAsync_WhenVersionChanged_ReturnsConcurrencyFailure()
    {
        var store = new FakePaperTradingStore
        {
            PositionById = Position("p1"),
            TryCloseResult = false
        };
        var sut = CreateExecutor(store: store);

        var result = await sut.CloseAtPriceAsync("BOT8012", "p1", 110m, "manual", default);

        Assert.False(result.Succeeded);
        Assert.Contains("changed concurrently", result.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, store.TryCloseCalls);
    }

    private static PaperTradeExecutor CreateExecutor(
        FakePaperTradingStore? store = null,
        FakeRuntimeConfigurationProvider? configProvider = null,
        FakeSignalContextAccessor? signalContext = null,
        FakeMarketPriceProvider? prices = null,
        decimal markPrice = 100m,
        bool enabled = true,
        int maximumOpenPositions = 100,
        decimal slippagePercent = 0m,
        decimal commissionPercent = 0m,
        decimal defaultTakeProfitPercent = 0.5m,
        decimal defaultStopLossPercent = 0.75m)
        => new(
            store ?? new FakePaperTradingStore(),
            prices ?? new FakeMarketPriceProvider(markPrice),
            configProvider ?? new FakeRuntimeConfigurationProvider(Runtime()),
            new NullRecorder(),
            new FakeEventStore(),
            signalContext ?? new FakeSignalContextAccessor(null),
            Options.Create(new PaperTradingOptions
            {
                Enabled = enabled,
                InitialBalance = 10_000m,
                CommissionPercent = commissionPercent,
                SlippagePercent = slippagePercent,
                DefaultTakeProfitPercent = defaultTakeProfitPercent,
                DefaultStopLossPercent = defaultStopLossPercent,
                PricePollMilliseconds = 1000,
                MaximumOpenPositions = maximumOpenPositions
            }),
            NullLogger<PaperTradeExecutor>.Instance);

    private static BotRuntimeConfiguration Runtime(
        decimal quantity = 1m,
        decimal? profitDistance = 10m,
        string environment = "Paper")
        => new(
            "BOT8012",
            "Grid",
            "BTCUSDC",
            environment,
            "Internal",
            true,
            true,
            quantity,
            50,
            400m,
            profitDistance,
            2,
            180,
            1,
            Now,
            false);

    private static PaperTradingPosition Position(
        string shortId,
        PositionSide side = PositionSide.Long,
        decimal quantity = 1m,
        decimal entryPrice = 100m,
        decimal entryFee = 0m)
        => new()
        {
            PositionId = Guid.NewGuid(),
            ShortId = shortId,
            SignalId = "signal-1",
            StrategyVersion = "1.0.0",
            BotName = "BOT8012",
            Symbol = "BTCUSDC",
            Side = side,
            Quantity = quantity,
            EntryPrice = entryPrice,
            TakeProfitPrice = side == PositionSide.Long ? 110m : 90m,
            StopLossPrice = side == PositionSide.Long ? 90m : 110m,
            EntryFee = entryFee,
            Status = PaperPositionStatus.Open,
            Source = "test",
            OpenedAtUtc = Now,
            Version = 1
        };

    private sealed class FakePaperTradingStore : IPaperTradingStore
    {
        public IReadOnlyCollection<PaperTradingPosition> OpenPositions { get; set; } = [];
        public PaperTradingPosition? PositionById { get; set; }
        public bool TryCloseResult { get; set; } = true;
        public int CreateCalls { get; private set; }
        public int TryCloseCalls { get; private set; }
        public List<PaperTradingPosition> Created { get; } = [];
        public decimal? LastExitPrice { get; private set; }
        public decimal? LastExitFee { get; private set; }
        public decimal? LastRealizedPnl { get; private set; }
        public string? LastCloseReason { get; private set; }

        public Task CreateAsync(PaperTradingPosition position, CancellationToken ct)
        {
            CreateCalls++;
            Created.Add(position);
            return Task.CompletedTask;
        }

        public Task<PaperTradingAccount> GetAccountAsync(decimal initialBalance, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<PaperTradingPosition?> GetAsync(string botName, string shortId, CancellationToken ct)
            => Task.FromResult(PositionById);

        public Task<IReadOnlyCollection<PaperTradingPosition>> GetOpenAsync(CancellationToken ct)
            => Task.FromResult(OpenPositions);

        public Task<IReadOnlyCollection<PaperTradingPosition>> QueryAsync(
            string? botName,
            string? symbol,
            PaperPositionStatus? status,
            int skip,
            int take,
            CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<PaperTradingPosition>>([]);

        public Task ResetAsync(string actor, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryCloseAsync(
            Guid id,
            long expectedVersion,
            decimal exitPrice,
            decimal exitFee,
            decimal pnl,
            string reason,
            DateTime closedAt,
            CancellationToken ct)
        {
            TryCloseCalls++;
            LastExitPrice = exitPrice;
            LastExitFee = exitFee;
            LastRealizedPnl = pnl;
            LastCloseReason = reason;
            return Task.FromResult(TryCloseResult);
        }
    }

    private sealed class FakeMarketPriceProvider(decimal price) : IMarketPriceProvider
    {
        public int Calls { get; private set; }

        public Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(price);
        }
    }

    private sealed class FakeRuntimeConfigurationProvider(BotRuntimeConfiguration? configuration)
        : IBotRuntimeConfigurationProvider
    {
        public Task<BotRuntimeConfiguration?> GetAsync(string botName, CancellationToken ct)
            => Task.FromResult(configuration);

        public void Set(BotRuntimeConfiguration configuration) { }
    }

    private sealed class FakeSignalContextAccessor(TradingSignalExecutionContext? current)
        : ITradingSignalContextAccessor
    {
        public TradingSignalExecutionContext? Current { get; } = current;

        public IDisposable Push(TradingSignalExecutionContext context)
            => new NoopDisposable();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    private sealed class NullRecorder : ITradingPipelineRecorder
    {
        public Task RecordSignalAsync(SignalHistoryRecord record, CancellationToken ct) => Task.CompletedTask;
        public Task RecordDecisionAsync(DecisionHistoryRecord record, CancellationToken ct) => Task.CompletedTask;
        public Task UpsertPositionAsync(PositionHistoryRecord record, CancellationToken ct) => Task.CompletedTask;
        public Task RecordPositionEventAsync(PositionEventHistoryRecord record, CancellationToken ct) => Task.CompletedTask;
        public Task RecordOrderEventAsync(OrderEventHistoryRecord record, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeEventStore : ITradingEventStore
    {
        public Task<StoredTradingEvent> AppendAsync(AppendTradingEvent request, CancellationToken ct)
            => Task.FromResult<StoredTradingEvent>(null!);

        public Task<IReadOnlyList<StoredTradingEvent>> ReadAsync(EventStoreQuery query, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<StoredTradingEvent>>([]);

        public Task<IReadOnlyList<StoredTradingEvent>> ReadStreamAsync(
            string aggregateType,
            string aggregateId,
            long afterVersion,
            int take,
            CancellationToken ct)
            => Task.FromResult<IReadOnlyList<StoredTradingEvent>>([]);
    }
}
