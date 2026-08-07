namespace TradingSystem.ReplayEngine.Models;

public sealed record ReplayStepResult(
    Guid ReplayId,
    long GlobalPosition,
    Guid SourceEventId,
    string EventType,
    DateTime VirtualTimeUtc,
    bool Succeeded,
    string ResultJson,
    string? Error);
