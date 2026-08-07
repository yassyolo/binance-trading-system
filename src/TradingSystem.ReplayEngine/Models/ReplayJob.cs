using TradingSystem.ReplayEngine.Models.Enums;

namespace TradingSystem.ReplayEngine.Models;

public sealed record ReplayJob(
    Guid ReplayId,
    string Name,
    ReplayMode Mode,
    ReplayJobStatus Status,
    string RequestedBy,
    CreateReplayRequest Request,
    long LastGlobalPosition,
    long ProcessedEvents,
    long FailedEvents,
    int ProgressPercent,
    string? ProgressStage,
    string? Error,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? DeterministicHash);
