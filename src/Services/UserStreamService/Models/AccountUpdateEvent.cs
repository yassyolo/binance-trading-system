namespace UserStreamService.Models;

public sealed record AccountUpdateEvent
{
    public string EventReasonType { get; init; } = string.Empty;
}