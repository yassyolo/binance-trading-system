using TradingSystem.BotRuntime.Runtime;

namespace TradingSystem.BotRuntime.Tests;

public sealed class BotRuntimeStateTests
{
    [Theory]
    [InlineData(BotRuntimeStatus.Running, true, true)]
    [InlineData(BotRuntimeStatus.Running, false, false)]
    [InlineData(BotRuntimeStatus.Paused, true, false)]
    [InlineData(BotRuntimeStatus.Stopped, true, false)]
    [InlineData(BotRuntimeStatus.EmergencyStopped, true, false)]
    public void AcceptsNewSignals_ShouldRequireRunningAndExecutionEnabled(BotRuntimeStatus status, bool executionEnabled, bool expected)
    {
        var state = new BotRuntimeState("BOT8012", status, 1, DateTime.UtcNow, "test", null, executionEnabled);
        Assert.Equal(expected, state.AcceptsNewSignals);
    }
}
