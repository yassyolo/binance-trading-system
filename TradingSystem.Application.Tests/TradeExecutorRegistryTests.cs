using TradingSystem.Application.Execution;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Domain.Enums;
using Xunit;

namespace TradingSystem.Application.Tests;

public sealed class TradeExecutorRegistryTests
{
    [Fact]
    public async Task OpenAsync_DelegatesToMatchingExecutorCaseInsensitive()
    {
        var executor = new FakeExecutor("BOT8012");
        var sut = new TradeExecutorRegistry([executor]);

        var result = await sut.OpenAsync("bot8012", "BTCUSDC", PositionSide.Long, "test", default);

        Assert.True(result.Succeeded);
        Assert.Equal(1, executor.OpenCalls);
    }

    [Fact]
    public async Task CloseAsync_DelegatesToMatchingExecutor()
    {
        var executor = new FakeExecutor("BOT8012");
        var sut = new TradeExecutorRegistry([executor]);

        var result = await sut.CloseAsync("BOT8012", "p1", "manual", default);

        Assert.True(result.Succeeded);
        Assert.Equal(1, executor.CloseCalls);
    }

    [Fact]
    public async Task OpenAsync_WhenExecutorMissing_Throws()
    {
        var sut = new TradeExecutorRegistry([]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.OpenAsync("missing", "BTCUSDC", PositionSide.Long, null, default));
    }

    [Fact]
    public void Constructor_DuplicateBotNames_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new TradeExecutorRegistry([new FakeExecutor("BOT1"), new FakeExecutor("bot1")]));
    }

    private sealed class FakeExecutor(string botName) : IBotTradeExecutor
    {
        public string BotName => botName;
        public int OpenCalls { get; private set; }
        public int CloseCalls { get; private set; }

        public Task<TradeExecutionResult> OpenAsync(string symbol, PositionSide side, string? source, CancellationToken ct)
        {
            OpenCalls++;
            return Task.FromResult(TradeExecutionResult.Success("p1"));
        }

        public Task<TradeExecutionResult> CloseAsync(string shortId, string reason, CancellationToken ct)
        {
            CloseCalls++;
            return Task.FromResult(TradeExecutionResult.Success(shortId));
        }
    }
}
