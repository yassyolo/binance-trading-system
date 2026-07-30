using System.Text.Json;

namespace TradingSystem.BotRuntime.Commands;

public enum BotCommandType { Start,  Stop,  Pause,  Resume,  EmergencyStop,  ClosePosition,  CancelTakeProfit,  RecreateTakeProfit }
public enum BotCommandStatus { Pending,  Processing,  Completed,  Failed,  Rejected }

public sealed record BotCommand(
    Guid CommandId, 
    string BotName, 
    BotCommandType Command, 
    BotCommandStatus Status, 
    string RequestedBy, 
    string Reason, 
    JsonDocument Payload, 
    DateTime RequestedAtUtc, 
    int AttemptCount);

public interface IBotCommandQueue
{
    Task<IReadOnlyCollection<BotCommand>> ClaimPendingAsync(string workerId,  int batchSize,  TimeSpan processingTimeout,  CancellationToken ct);
    Task CompleteAsync(Guid commandId,  string workerId,  CancellationToken ct);
    Task FailAsync(Guid commandId,  string workerId,  string error,  bool retryable,  CancellationToken ct);
    Task RejectAsync(Guid commandId,  string workerId,  string reason,  CancellationToken ct);
}
