using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012;
using StrategyService.Bots.Bot8012.Configuration;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;
using Xunit;

namespace StrategyService.Tests;

public sealed class TpOnlyGridOrderEventHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleTpFilledAsync_FullQuantity_ClosesPosition()
    {
        var position = Position(quantity: 0.002m, remaining: 0.002m);
        var store = new FakePositionStore(position);
        var sut = CreateSut(store);

        await sut.HandleTpFilledAsync(position.ShortId, 0.002m, default);

        Assert.True(position.Closed);
        Assert.Equal(0m, position.RemainingQuantity);
        Assert.True(position.TpExecuted);
        Assert.Equal("FILLED", position.TpStatus);
        Assert.Equal(Now, position.TpFilledAtUtc);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task HandleTpFilledAsync_PartialQuantity_LeavesPositionOpen()
    {
        var position = Position(quantity: 0.002m, remaining: 0.002m);
        var store = new FakePositionStore(position);
        var sut = CreateSut(store);

        await sut.HandleTpFilledAsync(position.ShortId, 0.001m, default);

        Assert.False(position.Closed);
        Assert.Equal(0.001m, position.RemainingQuantity);
        Assert.True(position.TpExecuted);
        Assert.Equal(PositionStatus.Open, position.Status);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task HandleTpFilledAsync_MissingPosition_DoesNothing()
    {
        var store = new FakePositionStore(null);
        var sut = CreateSut(store);

        await sut.HandleTpFilledAsync("missing", 0.001m, default);

        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task HandleTpFilledAsync_ClosedPosition_DoesNothing()
    {
        var position = Position(quantity: 0.002m, remaining: 0m);
        position.MarkClosed("test", Now.AddMinutes(-1));

        var store = new FakePositionStore(position);
        var sut = CreateSut(store);

        await sut.HandleTpFilledAsync(position.ShortId, 0.001m, default);

        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task HandleTpTerminalAsync_UpdatesTerminalStatusAndProtectionFlag()
    {
        var position = Position(quantity: 0.002m, remaining: 0.002m);
        position.ProtectiveActive = true;

        var store = new FakePositionStore(position);
        var sut = CreateSut(store);

        await sut.HandleTpTerminalAsync(position.ShortId, "CANCELED", default);

        Assert.Equal("CANCELED", position.TpStatus);
        Assert.False(position.ProtectiveActive);
        Assert.Equal(Now, position.UpdatedAtUtc);
        Assert.Equal(1, store.SaveCalls);
    }

    [Theory]
    [InlineData("EXPIRED")]
    [InlineData("REJECTED")]
    public async Task HandleTpTerminalAsync_SupportedTerminalStatuses_ArePersisted(string status)
    {
        var position = Position(quantity: 0.002m, remaining: 0.002m);
        var store = new FakePositionStore(position);
        var sut = CreateSut(store);

        await sut.HandleTpTerminalAsync(position.ShortId, status, default);

        Assert.Equal(status, position.TpStatus);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task HandleSlTriggeredAsync_TpOnlyHandler_IsNoOp()
    {
        var position = Position(quantity: 0.002m, remaining: 0.002m);
        var store = new FakePositionStore(position);
        var sut = CreateSut(store);

        await sut.HandleSlTriggeredAsync(position.ShortId, default);

        Assert.Equal(0, store.SaveCalls);
        Assert.False(position.Closed);
    }

    [Fact]
    public async Task HandleStop3TriggeredAsync_TpOnlyHandler_IsNoOp()
    {
        var position = Position(quantity: 0.002m, remaining: 0.002m);
        var store = new FakePositionStore(position);
        var sut = CreateSut(store);

        await sut.HandleStop3TriggeredAsync(position.ShortId, default);

        Assert.Equal(0, store.SaveCalls);
        Assert.False(position.Closed);
    }

    private static Bot8012OrderEventHandler CreateSut(FakePositionStore store)
        => new(
            Options.Create(new Bot8012Options { BotName = "BOT8012" }),
            store,
            new TestClock(Now));

    private static BotPosition Position(decimal quantity, decimal remaining)
        => new()
        {
            ShortId = "abc12345",
            BotName = "BOT8012",
            Symbol = "BTCUSDC",
            Side = PositionSide.Long,
            Mode = PositionMode.TpOnly,
            Quantity = quantity,
            RemainingQuantity = remaining,
            EntryPrice = 60_000m,
            TpPrice = 60_200m,
            TpStatus = "NEW",
            ProtectiveActive = true,
            Status = PositionStatus.Open,
            CreatedAtUtc = Now.AddMinutes(-5)
        };

    private sealed class FakePositionStore(BotPosition? position) : IPositionStore
    {
        public int SaveCalls { get; private set; }

        public Task SaveAsync(BotPosition value, CancellationToken ct)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }

        public Task<BotPosition?> GetAsync(string botName, string shortId, CancellationToken ct)
            => Task.FromResult(position);

        public Task<IReadOnlyCollection<BotPosition>> GetAllAsync(string botName, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<BotPosition>>(position is null ? [] : [position]);
    }

    private sealed class TestClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
