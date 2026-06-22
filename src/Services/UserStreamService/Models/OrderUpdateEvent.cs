namespace UserStreamService.Models;

public sealed record OrderUpdateEvent
{
    public string ClientOrderId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PositionSide { get; init; } = string.Empty;
}