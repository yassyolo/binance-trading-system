namespace UserStreamService.Models;

public sealed record UserStreamEvent
{
    public string EventType { get; init; } = string.Empty;
    public string RawJson { get; init; } = string.Empty;
    public DateTime ReceivedAtUtc { get; init; } = DateTime.UtcNow;
}