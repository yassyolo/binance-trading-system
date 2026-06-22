namespace UserStreamService.Models;

public sealed record HealingSnapshot
{
    public string Type { get; init; } = "healing_snapshot";
    public string Reason { get; init; } = "reconnected";
    public string Symbol { get; init; } = string.Empty;
    public double DowntimeSeconds { get; init; }
    public List<string> ActiveClientIds { get; init; } = [];
}