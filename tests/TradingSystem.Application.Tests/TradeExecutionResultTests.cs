using TradingSystem.Application.Execution.Models;
using Xunit;

namespace TradingSystem.Application.Tests;

public sealed class TradeExecutionResultTests
{
    [Fact]
    public void Success_SetsExpectedValues()
    {
        var result = TradeExecutionResult.Success("p1", "opened");

        Assert.True(result.Succeeded);
        Assert.Equal("p1", result.ShortId);
        Assert.Equal("opened", result.Reason);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Failure_SetsExpectedValues()
    {
        var exception = new InvalidOperationException("boom");
        var result = TradeExecutionResult.Failure("failed", exception);

        Assert.False(result.Succeeded);
        Assert.Null(result.ShortId);
        Assert.Equal("failed", result.Reason);
        Assert.Same(exception, result.Exception);
    }
}
