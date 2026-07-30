namespace TradingSystem.BotRuntime.Runtime;

public enum BotRuntimeStatus
{
    Stopped  =  0, 
    Running  =  1, 
    Paused  =  2, 
    EmergencyStopped  =  3, 
    Faulted  =  4
}

public sealed record BotRuntimeState(
    string BotName, 
    BotRuntimeStatus Status, 
    long Version, 
    DateTime UpdatedAtUtc, 
    string UpdatedBy, 
    string? Reason, 
    bool ExecutionEnabled)
{
    public bool AcceptsNewSignals  =>  Status == BotRuntimeStatus.Running  &&  ExecutionEnabled;
}

public interface IBotRuntimeStateStore
{
    Task<BotRuntimeState?> GetAsync(string botName,  CancellationToken ct);
    Task<IReadOnlyCollection<BotRuntimeState>> GetAllAsync(CancellationToken ct);
    Task<BotRuntimeState> TransitionAsync(string botName,  BotRuntimeStatus status,  long expectedVersion,  string user,  string reason,  bool executionEnabled,  CancellationToken ct);
}

public interface IBotRuntimeStateProvider
{
    Task<BotRuntimeState> GetRequiredAsync(string botName,  CancellationToken ct);
    void Invalidate(string botName);
}
