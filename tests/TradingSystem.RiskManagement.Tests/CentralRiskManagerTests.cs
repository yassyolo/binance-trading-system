using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Risk.Contracts;
using TradingSystem.Application.Risk.Models;
using TradingSystem.Application.Time;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;
using TradingSystem.PortfolioManagement;
using TradingSystem.PortfolioManagement.Models;
using TradingSystem.PortfolioManagement.Provider;
using TradingSystem.RiskManagement;
using TradingSystem.RiskManagement.Configuration;
using TradingSystem.RiskManagement.Contracts;
using TradingSystem.RiskManagement.Models;
using TradingSystem.RiskManagement.Services;
using Xunit;

namespace TradingSystem.RiskManagement.Tests;

public sealed class CentralRiskManagerTests
{
    private static readonly DateTime Now =
        new(
            2026,
            8,
            8,
            10,
            0,
            0,
            DateTimeKind.Utc);

    [Fact]
    public async Task EvaluateOpenAsync_WhenDailyLossLimitReached_ShouldBlock()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions
            {
                MaximumDailyLoss = 100m
            },
            riskState: new RiskStateSnapshot(
                DailyRealizedPnl: -100m,
                DailyPeakEquity: 1_000m,
                CurrentEquity: 900m,
                ConsecutiveLosses: 0,
                HasCriticalReconciliationFindings: false));

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "DAILY_LOSS_LIMIT",
            result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenDailyLossIsBelowLimit_ShouldNotBlockForDailyLoss()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions
            {
                MaximumDailyLoss = 100m
            },
            riskState: new RiskStateSnapshot(
                DailyRealizedPnl: -99m,
                DailyPeakEquity: 1_000m,
                CurrentEquity: 999m,
                ConsecutiveLosses: 0,
                HasCriticalReconciliationFindings: false));

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenRiskManagementDisabled_ShouldAllow()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions
            {
                Enabled = false
            });

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenOrderQuantityIsZero_ShouldBlock()
    {
        var sut = CreateSut(
            orderSize: new RiskOrderSize(
                Quantity: 0m,
                Leverage: 1,
                MaximumNotional: null));

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "ORDER_SIZE_UNKNOWN",
            result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenMarkPriceProducesInvalidNotional_ShouldBlock()
    {
        var sut = CreateSut();

        var context = Context() with
        {
            MarkPrice = 0m
        };

        var result = await sut.EvaluateOpenAsync(
            context,
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "INVALID_NOTIONAL",
            result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenBotMaximumNotionalExceeded_ShouldBlock()
    {
        var sut = CreateSut(
            orderSize: new RiskOrderSize(
                Quantity: 2m,
                Leverage: 1,
                MaximumNotional: 150m));

        var result = await sut.EvaluateOpenAsync(
            Context(markPrice: 100m),
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "BOT_NOTIONAL_LIMIT",
            result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenCriticalReconciliationFindingExists_ShouldBlock()
    {
        var sut = CreateSut(
            riskState: new RiskStateSnapshot(
                DailyRealizedPnl: 0m,
                DailyPeakEquity: 1_000m,
                CurrentEquity: 1_000m,
                ConsecutiveLosses: 0,
                HasCriticalReconciliationFindings: true));

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "CRITICAL_RECONCILIATION",
            result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenDailyDrawdownLimitReached_ShouldBlock()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions
            {
                MaximumDailyLoss = 500m,
                MaximumDailyDrawdownPercent = 10m
            },
            riskState: new RiskStateSnapshot(
                DailyRealizedPnl: 0m,
                DailyPeakEquity: 1_000m,
                CurrentEquity: 900m,
                ConsecutiveLosses: 0,
                HasCriticalReconciliationFindings: false));

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "DAILY_DRAWDOWN_LIMIT",
            result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenConsecutiveLossLimitReached_ShouldBlock()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions
            {
                MaximumConsecutiveLosses = 5
            },
            riskState: new RiskStateSnapshot(
                DailyRealizedPnl: 0m,
                DailyPeakEquity: 1_000m,
                CurrentEquity: 1_000m,
                ConsecutiveLosses: 5,
                HasCriticalReconciliationFindings: false));

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "CONSECUTIVE_LOSSES_LIMIT",
            result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenMaximumOpenPositionsReached_ShouldBlock()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions
            {
                MaximumOpenPositions = 1
            },
            portfolio: EmptyPortfolio(
                openPositions: 1));

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "MAX_OPEN_POSITIONS",
            result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenProjectedNotionalExceedsMaximum_ShouldBlock()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions
            {
                MaximumEstimatedNotional = 1_050m
            },
            portfolio: EmptyPortfolio(
                grossNotional: 1_000m),
            orderSize: new RiskOrderSize(
                Quantity: 1m,
                Leverage: 1,
                MaximumNotional: null));

        var result = await sut.EvaluateOpenAsync(
            Context(markPrice: 100m),
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "MAX_ESTIMATED_NOTIONAL",
            result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenAllChecksPass_ShouldAllow()
    {
        var reservations =
            new InMemoryRiskAdmissionReservationStore();

        var sut = CreateSut(
            reservations: reservations);

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.True(result.Allowed);

        var active = reservations.GetActive(Now);

        Assert.Single(active);

        var reservation = active.Single();

        Assert.Equal(
            "BOT8012",
            reservation.BotName);

        Assert.Equal(
            "BTCUSDC",
            reservation.Symbol);

        Assert.Equal(
            PositionSide.Long,
            reservation.Side);

        Assert.Equal(
            1m,
            reservation.Quantity);

        Assert.Equal(
            100m,
            reservation.Notional);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenOrderSizeIsMissing_ShouldBlock()
    {
        var sut = CreateSut(
            sizingProvider:
                new NullOrderSizingProvider());

        var result = await sut.EvaluateOpenAsync(
            Context(),
            default);

        Assert.False(result.Allowed);
        Assert.Equal(
            "ORDER_SIZE_UNKNOWN",
            result.Code);
    }

    private static CentralRiskManager CreateSut(
    CentralRiskOptions? options = null,
    PortfolioSnapshot? portfolio = null,
    RiskOrderSize? orderSize = null,
    RiskStateSnapshot? riskState = null,
    IRiskAdmissionReservationStore? reservations = null,
    IRiskOrderSizingProvider? sizingProvider = null)
    {
        options ??= DefaultOptions();
        portfolio ??= EmptyPortfolio();

        sizingProvider ??=
            new OrderSizingProvider(
                orderSize ??
                new RiskOrderSize(
                    Quantity: 1m,
                    Leverage: 1,
                    MaximumNotional: null));

        return new CentralRiskManager(
            Options.Create(options),
            new PortfolioProvider(portfolio),
            sizingProvider,
            new StateProvider(
                riskState ??
                new RiskStateSnapshot(
                    DailyRealizedPnl: 0m,
                    DailyPeakEquity: 10_000m,
                    CurrentEquity: 10_000m,
                    ConsecutiveLosses: 0,
                    HasCriticalReconciliationFindings: false)),
            reservations ??
                new InMemoryRiskAdmissionReservationStore(),
            new TestClock(Now),
            NullLogger<CentralRiskManager>.Instance);
    }

    private static CentralRiskOptions DefaultOptions() =>
        new()
        {
            Enabled = true,
            MaximumOpenPositions = 8,
            MaximumOpenPositionsPerBot = 4,
            MaximumOpenPositionsPerSymbol = 6,
            MaximumEstimatedNotional = 100_000m,
            MaximumGrossNotionalPerSymbol = 0m,
            MaximumAbsoluteNetNotionalPerSymbol = 0m,
            MaximumDailyLoss = 500m,
            MaximumDailyDrawdownPercent = 5m,
            MaximumConsecutiveLosses = 5,
            BlockWhenReconciliationHasCriticalFindings = true,
            AdmissionReservationSeconds = 30
        };

    private static RiskEvaluationContext Context(
        decimal markPrice = 100m) =>
        new()
        {
            Signal = new TradeSignal
            {
                SignalId = "signal-1",
                BotName = "BOT8012",
                Symbol = "BTCUSDC",
                Side = PositionSide.Long,
                Source = "test",
                GeneratedAtUtc = Now
            },
            MarkPrice = markPrice,
            ActivePositions = [],
            EvaluatedAtUtc = Now
        };

    private static PortfolioSnapshot EmptyPortfolio(
        int openPositions = 0,
        decimal grossNotional = 0m) =>
        new(
            GeneratedAtUtc: Now,
            StartingEquity: 10_000m,
            RealizedPnlToday: 0m,
            UnrealizedPnl: 0m,
            Equity: 10_000m,
            PeakEquityToday: 10_000m,
            DailyDrawdown: 0m,
            DailyDrawdownPercent: 0m,
            ConsecutiveLosses: 0,
            OpenPositions: openPositions,
            GrossNotional: grossNotional,
            NetNotional: grossNotional,
            EstimatedInitialMargin: 0m,
            Positions: [],
            Symbols: [],
            Bots: []);

    private sealed class PortfolioProvider(
        PortfolioSnapshot snapshot)
        : IPortfolioSnapshotProvider
    {
        public Task<PortfolioSnapshot> GetSnapshotAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);

        public void Invalidate()
        {
            throw new NotImplementedException();
        }
    }

    private sealed class OrderSizingProvider(
        RiskOrderSize orderSize)
        : IRiskOrderSizingProvider
    {
        public Task<RiskOrderSize?> GetAsync(
            string botName,
            CancellationToken cancellationToken) =>
            Task.FromResult<RiskOrderSize?>(orderSize);
    }

    private sealed class NullOrderSizingProvider
        : IRiskOrderSizingProvider
    {
        public Task<RiskOrderSize?> GetAsync(
            string botName,
            CancellationToken cancellationToken) =>
            Task.FromResult<RiskOrderSize?>(null);
    }

    private sealed class StateProvider(
        RiskStateSnapshot snapshot)
        : IRiskStateProvider
    {
        public Task<RiskStateSnapshot> GetAsync(
            DateTime atUtc,
            CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }

    private sealed class TestClock(
        DateTime utcNow)
        : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}