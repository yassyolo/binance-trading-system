using Microsoft.Extensions.Options;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.BotRuntime.Configuration.Models;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;
using TradingSystem.Reconciliation.Configuration;
using TradingSystem.Reconciliation.Contracts;
using TradingSystem.Reconciliation.Models;
using TradingSystem.Reconciliation.Models.Enums;
using TradingSystem.Reconciliation.Services;
using Xunit;

namespace TradingSystem.Reconciliation.Tests;

public sealed class PositionReconciliationServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RunAsync_WhenNoConfiguredBotIsLive_SavesEmptyRun()
    {
        var findings = new CapturingFindingStore();
        var sut = CreateSut(
            configs: new FakeConfigProvider(Runtime("BOT8012", "Paper")),
            findingStore: findings);

        var result = await sut.RunAsync(default);

        Assert.Empty(result.Findings);
        Assert.Empty(result.EvaluatedSymbols);
        Assert.Equal(0, result.HealedCount);
        Assert.Single(findings.Runs);
    }

    [Fact]
    public async Task RunAsync_ExchangeExposureWithoutLocalOwner_CreatesCriticalOrphanPosition()
    {
        var exchange = new FakeExchangeStateProvider(
            new ExchangeStateSnapshot(
                [new ExchangePositionSnapshot("BTCUSDC", "Long", 0.002m, 60_000m)],
                []));

        var result = await CreateSut(exchange: exchange).RunAsync(default);

        var finding = Assert.Single(result.Findings);
        Assert.Equal(ReconciliationFindingType.OrphanExchangePosition, finding.Type);
        Assert.Equal(ReconciliationSeverity.Critical, finding.Severity);
        Assert.Equal(HealingActionType.RequestManualReview, finding.SuggestedAction);
        Assert.False(finding.AutoHealAllowed);
    }

    [Fact]
    public async Task RunAsync_QuantityDifferenceWithinTolerance_DoesNotCreateQuantityFinding()
    {
        var local = Position("p1", PositionSide.Long, 0.002m);
        var exchange = new FakeExchangeStateProvider(
            new ExchangeStateSnapshot(
                [new ExchangePositionSnapshot("BTCUSDC", "Long", 0.002000005m, 60_000m)],
                [Order(local.TpClientId!, "LIMIT")]));

        var result = await CreateSut(
            positions: new FakePositionStore(local),
            exchange: exchange,
            quantityTolerance: 0.00000001m).RunAsync(default);

        Assert.DoesNotContain(result.Findings, x => x.Type == ReconciliationFindingType.QuantityMismatch);
    }

    [Fact]
    public async Task RunAsync_QuantityDifferenceAboveTolerance_CreatesCriticalMismatch()
    {
        var local = Position("p1", PositionSide.Long, 0.002m);
        var exchange = new FakeExchangeStateProvider(
            new ExchangeStateSnapshot(
                [new ExchangePositionSnapshot("BTCUSDC", "Long", 0.003m, 60_000m)],
                [Order(local.TpClientId!, "LIMIT")]));

        var result = await CreateSut(
            positions: new FakePositionStore(local),
            exchange: exchange).RunAsync(default);

        var finding = Assert.Single(result.Findings, x => x.Type == ReconciliationFindingType.QuantityMismatch);
        Assert.Equal(ReconciliationSeverity.Critical, finding.Severity);
        Assert.False(finding.AutoHealAllowed);
    }

    [Fact]
    public async Task RunAsync_MissingTakeProfit_CreatesCriticalFinding()
    {
        var local = Position("p1", PositionSide.Long, 0.002m);
        var exchange = new FakeExchangeStateProvider(
            new ExchangeStateSnapshot(
                [new ExchangePositionSnapshot("BTCUSDC", "Long", 0.002m, 60_000m)],
                []));

        var result = await CreateSut(
            positions: new FakePositionStore(local),
            exchange: exchange,
            autoHealProtectiveOrders: false).RunAsync(default);

        var finding = Assert.Single(result.Findings, x => x.Type == ReconciliationFindingType.MissingTakeProfit);
        Assert.Equal(ReconciliationSeverity.Critical, finding.Severity);
        Assert.Equal(HealingActionType.RecreateTakeProfit, finding.SuggestedAction);
        Assert.False(finding.AutoHealAllowed);
    }

    [Fact]
    public async Task RunAsync_MissingStopLoss_CreatesCriticalFinding()
    {
        var local = Position("p1", PositionSide.Long, 0.002m);
        local.ProtectiveActive = true;
        local.SlClientId = "BOT8012_SL_p1";
        local.SlExecuted = false;

        var exchange = new FakeExchangeStateProvider(
            new ExchangeStateSnapshot(
                [new ExchangePositionSnapshot("BTCUSDC", "Long", 0.002m, 60_000m)],
                [Order(local.TpClientId!, "LIMIT")]));

        var result = await CreateSut(
            positions: new FakePositionStore(local),
            exchange: exchange).RunAsync(default);

        var finding = Assert.Single(result.Findings, x => x.Type == ReconciliationFindingType.MissingStopLoss);
        Assert.Equal(ReconciliationSeverity.Critical, finding.Severity);
        Assert.Equal(HealingActionType.RecreateStopLoss, finding.SuggestedAction);
    }

    [Fact]
    public async Task RunAsync_UnknownBotOrder_CreatesCriticalOrphanOrder()
    {
        var exchange = new FakeExchangeStateProvider(
            new ExchangeStateSnapshot(
                [],
                [Order("BOT9999_TP_unknown", "LIMIT")]));

        var result = await CreateSut(exchange: exchange).RunAsync(default);

        var finding = Assert.Single(result.Findings, x => x.Type == ReconciliationFindingType.OrphanExchangeOrder);
        Assert.Equal("BOT9999", finding.BotName);
        Assert.Equal(ReconciliationSeverity.Critical, finding.Severity);
        Assert.False(finding.AutoHealAllowed);
    }

    [Fact]
    public async Task RunAsync_StaleLocalPosition_WhenAutoHealEnabled_ExecutesHealer()
    {
        var local = Position("p1", PositionSide.Long, 0.002m);
        local.TpClientId = null;

        var healer = new CapturingHealer(true);
        var result = await CreateSut(
            positions: new FakePositionStore(local),
            exchange: new FakeExchangeStateProvider(new ExchangeStateSnapshot([], [])),
            healer: healer,
            autoHealStaleLocalPositions: true).RunAsync(default);

        var finding = Assert.Single(result.Findings, x => x.Type == ReconciliationFindingType.StaleLocalPosition);
        Assert.True(finding.AutoHealAllowed);
        Assert.Equal(HealingActionType.DeleteStaleLocalPosition, finding.SuggestedAction);
        Assert.Equal(1, healer.Calls);
        Assert.Equal(1, result.HealedCount);
    }

    [Fact]
    public void EnumerateClientOrderIds_ReturnsOnlyNonEmptyKnownIds()
    {
        var position = Position("p1", PositionSide.Long, 0.002m);
        position.ParentClientId = "P";
        position.TpClientId = "TP";
        position.SlClientId = null;
        position.Stop3ClientId = "S3";
        position.CloseClientId = "";

        var result = PositionReconciliationService.EnumerateClientOrderIds(position).ToArray();

        Assert.Equal(["P", "TP", "S3"], result);
    }

    private static PositionReconciliationService CreateSut(
        FakePositionStore? positions = null,
        FakeExchangeStateProvider? exchange = null,
        CapturingHealer? healer = null,
        CapturingFindingStore? findingStore = null,
        FakeConfigProvider? configs = null,
        decimal quantityTolerance = 0.00000001m,
        bool autoHealStaleLocalPositions = true,
        bool autoHealProtectiveOrders = false)
        => new(
            Options.Create(new ReconciliationOptions
            {
                Enabled = true,
                IntervalSeconds = 30,
                AutoHealStaleLocalPositions = autoHealStaleLocalPositions,
                AutoHealProtectiveOrders = autoHealProtectiveOrders,
                QuantityTolerance = quantityTolerance,
                Bots = ["BOT8012"],
                Symbols = ["BTCUSDC"]
            }),
            positions ?? new FakePositionStore(),
            exchange ?? new FakeExchangeStateProvider(new ExchangeStateSnapshot([], [])),
            healer ?? new CapturingHealer(false),
            findingStore ?? new CapturingFindingStore(),
            configs ?? new FakeConfigProvider(Runtime("BOT8012", "Demo")));

    private static BotRuntimeConfiguration Runtime(string bot, string environment)
        => new(
            bot,
            "Grid",
            "BTCUSDC",
            environment,
            "Internal",
            true,
            true,
            0.002m,
            50,
            400m,
            200m,
            2,
            180,
            1,
            Now,
            false);

    private static BotPosition Position(string shortId, PositionSide side, decimal remaining)
        => new()
        {
            ShortId = shortId,
            BotName = "BOT8012",
            Symbol = "BTCUSDC",
            Side = side,
            Mode = PositionMode.TpOnly,
            Quantity = remaining,
            RemainingQuantity = remaining,
            EntryPrice = 60_000m,
            TpClientId = $"BOT8012_TP_{shortId}",
            TpPrice = side == PositionSide.Long ? 60_200m : 59_800m,
            TpExecuted = false,
            Closed = false,
            Status = PositionStatus.Open,
            CreatedAtUtc = Now
        };

    private static ExchangeOrderSnapshot Order(string clientId, string kind)
        => new("BTCUSDC", clientId, kind, 0.002m, null);

    private sealed class FakePositionStore(params BotPosition[] positions) : IPositionStore
    {
        public Task SaveAsync(BotPosition position, CancellationToken ct) => Task.CompletedTask;

        public Task<BotPosition?> GetAsync(string botName, string shortId, CancellationToken ct)
            => Task.FromResult(positions.FirstOrDefault(x =>
                x.BotName.Equals(botName, StringComparison.OrdinalIgnoreCase) &&
                x.ShortId.Equals(shortId, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyCollection<BotPosition>> GetAllAsync(string botName, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<BotPosition>>(
                positions.Where(x => x.BotName.Equals(botName, StringComparison.OrdinalIgnoreCase)).ToArray());
    }

    private sealed class FakeExchangeStateProvider(ExchangeStateSnapshot snapshot) : IExchangeStateProvider
    {
        public Task<ExchangeStateSnapshot> GetAsync(string symbol, CancellationToken ct)
            => Task.FromResult(snapshot);
    }

    private sealed class CapturingHealer(bool result) : IHealingActionExecutor
    {
        public int Calls { get; private set; }
        public List<ReconciliationFinding> Findings { get; } = [];

        public Task<bool> ExecuteAsync(ReconciliationFinding finding, CancellationToken ct)
        {
            Calls++;
            Findings.Add(finding);
            return Task.FromResult(result);
        }
    }

    private sealed class CapturingFindingStore : IReconciliationFindingStore
    {
        public List<ReconciliationRunResult> Runs { get; } = [];

        public Task SaveRunAsync(ReconciliationRunResult result, CancellationToken ct)
        {
            Runs.Add(result);
            return Task.CompletedTask;
        }

        public Task<bool> HasUnresolvedCriticalAsync(CancellationToken ct)
            => Task.FromResult(false);
    }

    private sealed class FakeConfigProvider(params BotRuntimeConfiguration[] configurations)
        : IBotRuntimeConfigurationProvider
    {
        public Task<BotRuntimeConfiguration?> GetAsync(string botName, CancellationToken ct)
            => Task.FromResult(configurations.FirstOrDefault(x =>
                x.BotName.Equals(botName, StringComparison.OrdinalIgnoreCase)));

        public void Set(BotRuntimeConfiguration configuration) { }
    }
}
