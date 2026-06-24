namespace StrategyService.Models;

public sealed class Signal
{
    public string Action { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public string Source { get; set; } = "webhook";

    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
}