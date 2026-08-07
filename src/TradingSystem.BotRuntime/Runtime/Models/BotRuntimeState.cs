using TradingSystem.BotRuntime.Runtime.Models.Enums;

namespace TradingSystem.BotRuntime.Runtime.Models;

public sealed record BotRuntimeState(
    string BotName,
    BotRuntimeStatus Status,
    long Version,
    DateTime UpdatedAtUtc,
    string UpdatedBy,
    string? Reason,
    bool ExecutionEnabled)
{
    public bool AcceptsNewSignals => Status == BotRuntimeStatus.Running && ExecutionEnabled;
}

