using TradingSystem.Application.Strategies.Models;
using TradingSystem.Application.Strategies.Models.Enums;
using TradingSystem.Domain.Enums;
using Xunit;

namespace TradingSystem.Application.Tests;

public sealed class StrategyDecisionTests
{
    [Fact]
    public void Open_SetsExpectedState()
    {
        var result = StrategyDecision.Open(PositionSide.Long, "ok");

        Assert.Equal(StrategyDecisionType.Open, result.Type);
        Assert.Equal(PositionSide.Long, result.Side);
        Assert.Equal("ok", result.Reason);
        Assert.True(result.ShouldOpen);
        Assert.False(result.ShouldClosePositions);
        Assert.Empty(result.PositionsToClose);
    }

    [Fact]
    public void Block_DoesNotOpen()
    {
        var result = StrategyDecision.Block(PositionSide.Short, "blocked");

        Assert.Equal(StrategyDecisionType.Ignore, result.Type);
        Assert.False(result.ShouldOpen);
        Assert.False(result.ShouldClosePositions);
    }

    [Fact]
    public void OpenAfterClosing_RequiresPositions()
    {
        Assert.Throws<ArgumentException>(() => StrategyDecision.OpenAfterClosing(PositionSide.Long, [], "reverse"));
    }

    [Fact]
    public void OpenAfterClosing_NullPositions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => StrategyDecision.OpenAfterClosing(PositionSide.Long, null!, "reverse"));
    }

    [Fact]
    public void OpenAfterClosing_SetsPositionsAndFlags()
    {
        var result = StrategyDecision.OpenAfterClosing(PositionSide.Short, ["p1", "p2"], "reverse");

        Assert.True(result.ShouldOpen);
        Assert.True(result.ShouldClosePositions);
        Assert.Equal(["p1", "p2"], result.PositionsToClose);
    }
}
