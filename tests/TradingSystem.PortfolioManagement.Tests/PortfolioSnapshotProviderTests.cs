using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;
using TradingSystem.PortfolioManagement.Configuration;
using TradingSystem.PortfolioManagement.Models;
using TradingSystem.PortfolioManagement.Performance;
using TradingSystem.PortfolioManagement.Position;
using TradingSystem.PortfolioManagement.Provider;
using Xunit;

namespace TradingSystem.PortfolioManagement.Tests;

public sealed class PortfolioSnapshotProviderTests
{
    private static readonly DateTime Now = new(2026, 8, 8, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetSnapshotAsync_LongAndShortPositions_CalculatesGrossAndNetNotional()
    {
        var paper = new FakePaperSource(
        [
            Position("long-1", PositionSide.Long, 2m, 90m),
            Position("short-1", PositionSide.Short, 1m, 110m)
        ]);

        var sut = CreateSut(paper: paper, price: 100m);

        var result = await sut.GetSnapshotAsync(default);

        Assert.Equal(2, result.OpenPositions);
        Assert.Equal(300m, result.GrossNotional);
        Assert.Equal(100m, result.NetNotional);

        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(200m, symbol.LongNotional);
        Assert.Equal(100m, symbol.ShortNotional);
        Assert.Equal(300m, symbol.GrossNotional);
        Assert.Equal(100m, symbol.NetNotional);
    }

    [Fact]
    public async Task GetSnapshotAsync_CalculatesLongAndShortUnrealizedPnl()
    {
        var paper = new FakePaperSource(
        [
            Position("long-1", PositionSide.Long, 2m, 90m),
            Position("short-1", PositionSide.Short, 1m, 110m)
        ]);

        var sut = CreateSut(paper: paper, price: 100m);

        var result = await sut.GetSnapshotAsync(default);

        Assert.Equal(30m, result.UnrealizedPnl);
        Assert.Equal(10_030m, result.Equity);
    }

    [Fact]
    public async Task GetSnapshotAsync_UsesConfiguredDefaultLeverageForEstimatedMargin()
    {
        var sut = CreateSut(
            paper: new FakePaperSource([Position("p1", PositionSide.Long, 2m, 90m)]),
            price: 100m,
            leverage: 50);

        var result = await sut.GetSnapshotAsync(default);

        var position = Assert.Single(result.Positions);
        Assert.Equal(4m, position.EstimatedInitialMargin);
        Assert.Equal(4m, result.EstimatedInitialMargin);
    }

    [Fact]
    public async Task GetSnapshotAsync_DuplicateRedisAndPaperIdentity_IsCountedOnce()
    {
        var redis = new FakePositionStore(
        [
            RedisPosition("same", PositionSide.Long, 1m, 90m)
        ]);

        var paper = new FakePaperSource(
        [
            Position("same", PositionSide.Long, 1m, 90m)
        ]);

        var sut = CreateSut(redis: redis, paper: paper, price: 100m);

        var result = await sut.GetSnapshotAsync(default);

        Assert.Single(result.Positions);
        Assert.Equal(1, result.OpenPositions);
        Assert.Equal(100m, result.GrossNotional);
    }

    [Fact]
    public async Task GetSnapshotAsync_ClosedAndZeroRemainingRedisPositions_AreExcluded()
    {
        var closed = RedisPosition("closed", PositionSide.Long, 1m, 90m);
        closed.Closed = true;

        var zeroRemaining = RedisPosition("zero", PositionSide.Long, 1m, 90m);
        zeroRemaining.RemainingQuantity = 0m;

        var open = RedisPosition("open", PositionSide.Long, 1m, 90m);

        var sut = CreateSut(
            redis: new FakePositionStore([closed, zeroRemaining, open]),
            price: 100m);

        var result = await sut.GetSnapshotAsync(default);

        var position = Assert.Single(result.Positions);
        Assert.Equal("open", position.PositionId);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithinCacheWindow_ReturnsSameSnapshotWithoutReload()
    {
        var clock = new MutableClock(Now);
        var paper = new FakePaperSource([Position("p1", PositionSide.Long, 1m, 90m)]);
        var sut = CreateSut(paper: paper, price: 100m, cacheMs: 500, clock: clock);

        var first = await sut.GetSnapshotAsync(default);
        clock.UtcNow = Now.AddMilliseconds(100);
        var second = await sut.GetSnapshotAsync(default);

        Assert.Same(first, second);
        Assert.Equal(1, paper.Calls);
    }

    [Fact]
    public async Task Invalidate_ForcesSnapshotRebuildBeforeCacheExpiry()
    {
        var clock = new MutableClock(Now);
        var paper = new FakePaperSource([Position("p1", PositionSide.Long, 1m, 90m)]);
        var sut = CreateSut(paper: paper, price: 100m, cacheMs: 500, clock: clock);

        var first = await sut.GetSnapshotAsync(default);
        sut.Invalidate();
        var second = await sut.GetSnapshotAsync(default);

        Assert.NotSame(first, second);
        Assert.Equal(2, paper.Calls);
    }

    [Fact]
    public async Task GetSnapshotAsync_InvalidMarkPrice_FailsClosedAfterConfiguredAttempts()
    {
        var prices = new FakePriceProvider(0m);
        var sut = CreateSut(
            paper: new FakePaperSource([Position("p1", PositionSide.Long, 1m, 90m)]),
            priceProvider: prices,
            retryCount: 2);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetSnapshotAsync(default));

        Assert.Contains("Risk evaluation must fail closed", error.Message);
        Assert.Equal(3, prices.Calls);
    }

    [Fact]
    public async Task GetSnapshotAsync_DisabledPortfolio_ReturnsEmptyBaseline()
    {
        var sut = CreateSut(enabled: false);

        var result = await sut.GetSnapshotAsync(default);

        Assert.Equal(10_000m, result.Equity);
        Assert.Equal(0, result.OpenPositions);
        Assert.Empty(result.Positions);
    }

    [Fact]
    public async Task GetSnapshotAsync_PerformanceSource_DrivesRealizedPnlPeakAndDrawdown()
    {
        var performance = new FakePerformanceSource(
            new PortfolioPerformanceSnapshot(
                RealizedPnlToday: -100m,
                PeakEquityToday: 10_500m,
                ConsecutiveLosses: 3,
                AsOfUtc: Now));

        var sut = CreateSut(
            paper: new FakePaperSource([Position("p1", PositionSide.Long, 1m, 100m)]),
            price: 100m,
            performance: performance);

        var result = await sut.GetSnapshotAsync(default);

        Assert.Equal(-100m, result.RealizedPnlToday);
        Assert.Equal(9_900m, result.Equity);
        Assert.Equal(10_500m, result.PeakEquityToday);
        Assert.Equal(600m, result.DailyDrawdown);
        Assert.Equal(600m / 10_500m * 100m, result.DailyDrawdownPercent);
        Assert.Equal(3, result.ConsecutiveLosses);
    }

    private static PortfolioSnapshotProvider CreateSut(
        FakePositionStore? redis = null,
        FakePaperSource? paper = null,
        decimal price = 100m,
        FakePriceProvider? priceProvider = null,
        int leverage = 50,
        int cacheMs = 0,
        int retryCount = 0,
        bool enabled = true,
        MutableClock? clock = null,
        IPortfolioPerformanceSource? performance = null)
        => new(
            Options.Create(new PortfolioOptions
            {
                Enabled = enabled,
                InitialEquity = 10_000m,
                SnapshotCacheMilliseconds = cacheMs,
                DefaultLeverage = leverage,
                LoadRetryCount = retryCount,
                LoadRetryDelayMilliseconds = 0,
                Bots = ["BOT8012"]
            }),
            redis ?? new FakePositionStore([]),
            paper ?? new FakePaperSource([]),
            priceProvider ?? new FakePriceProvider(price),
            performance ?? new NullPortfolioPerformanceSource(),
            clock ?? new MutableClock(Now),
            NullLogger<PortfolioSnapshotProvider>.Instance);

    private static PaperPortfolioPosition Position(
        string id,
        PositionSide side,
        decimal quantity,
        decimal entryPrice)
        => new("BOT8012", id, "BTCUSDC", side, quantity, entryPrice, Now.AddMinutes(-1));

    private static BotPosition RedisPosition(
        string id,
        PositionSide side,
        decimal quantity,
        decimal entryPrice)
        => new()
        {
            ShortId = id,
            BotName = "BOT8012",
            Symbol = "BTCUSDC",
            Side = side,
            Mode = PositionMode.TpOnly,
            Quantity = quantity,
            RemainingQuantity = quantity,
            EntryPrice = entryPrice,
            CreatedAtUtc = Now.AddMinutes(-1)
        };

    private sealed class FakePositionStore(IReadOnlyCollection<BotPosition> positions) : IPositionStore
    {
        public Task SaveAsync(BotPosition position, CancellationToken ct) => Task.CompletedTask;

        public Task<BotPosition?> GetAsync(string botName, string shortId, CancellationToken ct)
            => Task.FromResult(positions.FirstOrDefault(x => x.ShortId == shortId));

        public Task<IReadOnlyCollection<BotPosition>> GetAllAsync(string botName, CancellationToken ct)
            => Task.FromResult(positions);
    }

    private sealed class FakePaperSource(IReadOnlyCollection<PaperPortfolioPosition> positions) : IPaperPortfolioPositionSource
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyCollection<PaperPortfolioPosition>> GetOpenAsync(CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(positions);
        }
    }

    private sealed class FakePriceProvider(decimal price) : IMarketPriceProvider
    {
        public int Calls { get; private set; }

        public Task<decimal> GetMarkPriceAsync(string symbol, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(price);
        }
    }

    private sealed class FakePerformanceSource(PortfolioPerformanceSnapshot snapshot) : IPortfolioPerformanceSource
    {
        public Task<PortfolioPerformanceSnapshot> GetAsync(
            decimal currentUnrealizedPnl,
            decimal startingEquity,
            DateTime asOfUtc,
            CancellationToken ct)
            => Task.FromResult(snapshot);
    }

    private sealed class MutableClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }
}
