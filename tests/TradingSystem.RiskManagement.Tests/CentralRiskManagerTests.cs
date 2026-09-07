/*using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Risk.Contracts;
using TradingSystem.Application.Risk.Models;
using TradingSystem.Application.Time;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;
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
    private static readonly DateTime Now = new(2026, 8, 8, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EvaluateOpenAsync_WhenDailyLossLimitReached_ShouldBlock()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions { MaximumDailyLoss = 100m },
            riskState: new RiskStateSnapshot(
                DailyRealizedPnl: -100m,
                DailyPeakEquity: 1_000m,
                CurrentEquity: 900m,
                ConsecutiveLosses: 0,
                HasCriticalReconciliationFindings: false));

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("DAILY_LOSS_LIMIT", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenDailyLossIsBelowLimit_ShouldNotBlockForDailyLoss()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions { MaximumDailyLoss = 100m },
            riskState: new RiskStateSnapshot(
                DailyRealizedPnl: -99m,
                DailyPeakEquity: 1_000m,
                CurrentEquity: 999m,
                ConsecutiveLosses: 0,
                HasCriticalReconciliationFindings: false));

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenRiskManagementDisabled_ShouldAllow()
    {
        var sut = CreateSut(options: new CentralRiskOptions { Enabled = false });

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenOrderQuantityIsZero_ShouldBlock()
    {
        var sut = CreateSut(orderSize: new RiskOrderSize(Quantity: 0m, Leverage: 1, MaximumNotional: null));

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("ORDER_SIZE_UNKNOWN", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenMarkPriceProducesInvalidNotional_ShouldBlock()
    {
        var sut = CreateSut();
        var context = Context() with { MarkPrice = 0m };

        var result = await sut.EvaluateOpenAsync(context, default);

        Assert.False(result.Allowed);
        Assert.Equal("INVALID_NOTIONAL", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenBotMaximumNotionalExceeded_ShouldBlock()
    {
        var sut = CreateSut(orderSize: new RiskOrderSize(Quantity: 2m, Leverage: 1, MaximumNotional: 150m));

        var result = await sut.EvaluateOpenAsync(Context(markPrice: 100m), default);

        Assert.False(result.Allowed);
        Assert.Equal("BOT_NOTIONAL_LIMIT", result.Code);
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

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("CRITICAL_RECONCILIATION", result.Code);
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

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("DAILY_DRAWDOWN_LIMIT", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenConsecutiveLossLimitReached_ShouldBlock()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions { MaximumConsecutiveLosses = 5 },
            riskState: new RiskStateSnapshot(
                DailyRealizedPnl: 0m,
                DailyPeakEquity: 1_000m,
                CurrentEquity: 1_000m,
                ConsecutiveLosses: 5,
                HasCriticalReconciliationFindings: false));

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("CONSECUTIVE_LOSSES_LIMIT", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenMaximumOpenPositionsReached_ShouldBlock()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions { MaximumOpenPositions = 1 },
            portfolio: EmptyPortfolio(openPositions: 1));

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("MAX_OPEN_POSITIONS", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenProjectedNotionalExceedsMaximum_ShouldBlock()
    {
        var sut = CreateSut(
            options: new CentralRiskOptions { MaximumEstimatedNotional = 1_050m },
            portfolio: EmptyPortfolio(grossNotional: 1_000m),
            orderSize: new RiskOrderSize(Quantity: 1m, Leverage: 1, MaximumNotional: null));

        var result = await sut.EvaluateOpenAsync(Context(markPrice: 100m), default);

        Assert.False(result.Allowed);
        Assert.Equal("MAX_ESTIMATED_NOTIONAL", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenAllChecksPass_ShouldAllow()
    {
        var reservations = new InMemoryRiskAdmissionReservationStore();
        var sut = CreateSut(reservations: reservations);

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.True(result.Allowed);

        var active = reservations.GetActive(Now);
        var reservation = Assert.Single(active);

        Assert.Equal("BOT8012", reservation.BotName);
        Assert.Equal("BTCUSDC", reservation.Symbol);
        Assert.Equal(PositionSide.Long, reservation.Side);
        Assert.Equal(1m, reservation.Quantity);
        Assert.Equal(100m, reservation.Notional);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenOrderSizeIsMissing_ShouldBlock()
    {
        var sut = CreateSut(sizingProvider: new NullOrderSizingProvider());

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("ORDER_SIZE_UNKNOWN", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenMaximumOpenPositionsPerBotReached_ShouldBlock()
    {
        var portfolio = Portfolio(
            bots: [new BotExposureSnapshot("BOT8012", 2, 200m, 200m, 0m, 4m)]);

        var sut = CreateSut(
            options: Options(maxPerBot: 2),
            portfolio: portfolio);

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("MAX_OPEN_POSITIONS_PER_BOT", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenMaximumOpenPositionsPerSymbolReached_ShouldBlock()
    {
        var portfolio = Portfolio(
            symbols: [new SymbolExposureSnapshot("BTCUSDC", 3, 300m, 0m, 300m, 300m, 0m)]);

        var sut = CreateSut(
            options: Options(maxPerSymbol: 3),
            portfolio: portfolio);

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("MAX_OPEN_POSITIONS_PER_SYMBOL", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_WhenProjectedSymbolGrossExceedsLimit_ShouldBlock()
    {
        var portfolio = Portfolio(
            symbols: [new SymbolExposureSnapshot("BTCUSDC", 1, 900m, 0m, 900m, 900m, 0m)],
            grossNotional: 900m,
            netNotional: 900m);

        var sut = CreateSut(
            options: Options(maxSymbolGross: 950m),
            portfolio: portfolio,
            orderSize: new RiskOrderSize(1m, 1, null));

        var result = await sut.EvaluateOpenAsync(Context(markPrice: 100m), default);

        Assert.False(result.Allowed);
        Assert.Equal("MAX_SYMBOL_GROSS_NOTIONAL", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_LongCandidate_WhenProjectedAbsoluteNetExceedsLimit_ShouldBlock()
    {
        var portfolio = Portfolio(
            symbols: [new SymbolExposureSnapshot("BTCUSDC", 1, 900m, 0m, 900m, 900m, 0m)],
            grossNotional: 900m,
            netNotional: 900m);

        var sut = CreateSut(
            options: Options(maxSymbolNet: 950m),
            portfolio: portfolio,
            orderSize: new RiskOrderSize(1m, 1, null));

        var result = await sut.EvaluateOpenAsync(Context(markPrice: 100m), default);

        Assert.False(result.Allowed);
        Assert.Equal("MAX_SYMBOL_NET_NOTIONAL", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_ShortCandidate_CanReduceExistingPositiveNetExposure()
    {
        var portfolio = Portfolio(
            symbols: [new SymbolExposureSnapshot("BTCUSDC", 1, 900m, 0m, 900m, 900m, 0m)],
            grossNotional: 900m,
            netNotional: 900m);

        var sut = CreateSut(
            options: Options(maxSymbolNet: 950m, maxEstimatedNotional: 2_000m),
            portfolio: portfolio,
            orderSize: new RiskOrderSize(1m, 1, null));

        var result = await sut.EvaluateOpenAsync(
            Context(markPrice: 100m, side: PositionSide.Short),
            default);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task EvaluateOpenAsync_ActiveReservation_IsIncludedInGlobalPositionLimit()
    {
        var reservations = new InMemoryRiskAdmissionReservationStore();
        reservations.Add(Reservation("existing-signal", "BOT8013", "ETHUSDC", PositionSide.Long, 100m));

        var sut = CreateSut(
            options: Options(maxOpen: 1),
            reservations: reservations);

        var result = await sut.EvaluateOpenAsync(Context(), default);

        Assert.False(result.Allowed);
        Assert.Equal("MAX_OPEN_POSITIONS", result.Code);
    }

    [Fact]
    public async Task EvaluateOpenAsync_ActiveReservation_IsIncludedInProjectedGrossNotional()
    {
        var reservations = new InMemoryRiskAdmissionReservationStore();
        reservations.Add(Reservation("existing-signal", "BOT8013", "ETHUSDC", PositionSide.Long, 900m));

        var sut = CreateSut(
            options: Options(maxEstimatedNotional: 950m),
            reservations: reservations,
            orderSize: new RiskOrderSize(1m, 1, null));

        var result = await sut.EvaluateOpenAsync(Context(markPrice: 100m), default);

        Assert.False(result.Allowed);
        Assert.Equal("MAX_ESTIMATED_NOTIONAL", result.Code);
    }

    [Fact]
    public async Task CompleteAsync_WhenExecutionSucceeded_RemovesReservationAndInvalidatesPortfolio()
    {
        var reservations = new InMemoryRiskAdmissionReservationStore();
        reservations.Add(Reservation("signal-1", "BOT8012", "BTCUSDC", PositionSide.Long, 100m));

        var portfolio = new CapturingPortfolioProvider(Portfolio());
        var sut = CreateSut(
            portfolioProvider: portfolio,
            reservations: reservations);

        await sut.CompleteAsync("signal-1", true, default);

        Assert.Empty(reservations.GetActive(Now));
        Assert.Equal(1, portfolio.InvalidateCalls);
    }

    [Fact]
    public async Task CompleteAsync_WhenExecutionFailed_RemovesReservationWithoutInvalidatingPortfolio()
    {
        var reservations = new InMemoryRiskAdmissionReservationStore();
        reservations.Add(Reservation("signal-1", "BOT8012", "BTCUSDC", PositionSide.Long, 100m));

        var portfolio = new CapturingPortfolioProvider(Portfolio());
        var sut = CreateSut(
            portfolioProvider: portfolio,
            reservations: reservations);

        await sut.CompleteAsync("signal-1", false, default);

        Assert.Empty(reservations.GetActive(Now));
        Assert.Equal(0, portfolio.InvalidateCalls);
    }

    [Fact]
    public void ReservationStore_AddingSameSignal_ReplacesPreviousReservation()
    {
        var sut = new InMemoryRiskAdmissionReservationStore();

        sut.Add(Reservation("same-signal", "BOT8012", "BTCUSDC", PositionSide.Long, 100m));
        sut.Add(Reservation("SAME-SIGNAL", "BOT8012", "BTCUSDC", PositionSide.Long, 200m));

        var item = Assert.Single(sut.GetActive(Now));

        Assert.Equal(200m, item.Notional);
    }

    [Fact]
    public void ReservationStore_GetActive_RemovesExpiredReservations()
    {
        var sut = new InMemoryRiskAdmissionReservationStore();
        sut.Add(new RiskAdmissionReservation(
            Guid.NewGuid(),
            "expired",
            "BOT8012",
            "BTCUSDC",
            PositionSide.Long,
            1m,
            100m,
            Now));

        Assert.Empty(sut.GetActive(Now));
    }

    private static CentralRiskManager CreateSut(
        CentralRiskOptions? options = null,
        PortfolioSnapshot? portfolio = null,
        RiskOrderSize? orderSize = null,
        IRiskAdmissionReservationStore? reservations = null,
        CapturingPortfolioProvider? portfolioProvider = null,
        IRiskOrderSizingProvider? sizingProvider = null,
        RiskStateSnapshot? riskState = null)
    {
        portfolioProvider ??= new CapturingPortfolioProvider(portfolio ?? Portfolio());

        return new CentralRiskManager(
            Options.Create(options ?? Options()),
            portfolioProvider,
            sizingProvider ?? new OrderSizingProvider(orderSize ?? new RiskOrderSize(1m, 1, null)),
            new StateProvider(riskState ?? new RiskStateSnapshot(0m, 10_000m, 10_000m, 0, false)),
            reservations ?? new InMemoryRiskAdmissionReservationStore(),
            new TestClock(Now),
            NullLogger<CentralRiskManager>.Instance);
    }

    private static CentralRiskOptions Options(
        int maxOpen = 8,
        int maxPerBot = 4,
        int maxPerSymbol = 6,
        decimal maxEstimatedNotional = 100_000m,
        decimal maxSymbolGross = 0m,
        decimal maxSymbolNet = 0m)
        => new()
        {
            Enabled = true,
            MaximumOpenPositions = maxOpen,
            MaximumOpenPositionsPerBot = maxPerBot,
            MaximumOpenPositionsPerSymbol = maxPerSymbol,
            MaximumEstimatedNotional = maxEstimatedNotional,
            MaximumGrossNotionalPerSymbol = maxSymbolGross,
            MaximumAbsoluteNetNotionalPerSymbol = maxSymbolNet,
            MaximumDailyLoss = 500m,
            MaximumDailyDrawdownPercent = 5m,
            MaximumConsecutiveLosses = 5,
            BlockWhenReconciliationHasCriticalFindings = true,
            AdmissionReservationSeconds = 30
        };

    private static RiskEvaluationContext Context(
        decimal markPrice = 100m,
        PositionSide side = PositionSide.Long)
        => new()
        {
            Signal = new TradeSignal
            {
                SignalId = "signal-1",
                BotName = "BOT8012",
                Symbol = "BTCUSDC",
                Side = side,
                Source = "test",
                GeneratedAtUtc = Now
            },
            MarkPrice = markPrice,
            ActivePositions = [],
            EvaluatedAtUtc = Now
        };

    private static PortfolioSnapshot Portfolio(
        int openPositions = 0,
        decimal grossNotional = 0m,
        decimal netNotional = 0m,
        IReadOnlyCollection<SymbolExposureSnapshot>? symbols = null,
        IReadOnlyCollection<BotExposureSnapshot>? bots = null)
        => new(
            Now,
            10_000m,
            0m,
            0m,
            10_000m,
            10_000m,
            0m,
            0m,
            0,
            openPositions,
            grossNotional,
            netNotional,
            0m,
            [],
            symbols ?? [],
            bots ?? []);

    private static PortfolioSnapshot EmptyPortfolio(
        int openPositions = 0,
        decimal grossNotional = 0m)
        => new(
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

    private static RiskAdmissionReservation Reservation(
        string signalId,
        string botName,
        string symbol,
        PositionSide side,
        decimal notional)
        => new(
            Guid.NewGuid(),
            signalId,
            botName,
            symbol,
            side,
            1m,
            notional,
            Now.AddSeconds(30));

    private sealed class CapturingPortfolioProvider(PortfolioSnapshot snapshot) : IPortfolioSnapshotProvider
    {
        public int InvalidateCalls { get; private set; }

        public Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken ct)
            => Task.FromResult(snapshot);

        public void Invalidate() => InvalidateCalls++;
    }

    private sealed class OrderSizingProvider(RiskOrderSize size) : IRiskOrderSizingProvider
    {
        public Task<RiskOrderSize?> GetAsync(string botName, CancellationToken ct)
            => Task.FromResult<RiskOrderSize?>(size);
    }

    private sealed class StateProvider(RiskStateSnapshot snapshot) : IRiskStateProvider
    {
        public Task<RiskStateSnapshot> GetAsync(DateTime atUtc, CancellationToken ct)
            => Task.FromResult(snapshot);
    }

    private sealed class TestClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class NullOrderSizingProvider : IRiskOrderSizingProvider
    {
        public Task<RiskOrderSize?> GetAsync(string botName, CancellationToken ct)
            => Task.FromResult<RiskOrderSize?>(null);
    }
}
*/