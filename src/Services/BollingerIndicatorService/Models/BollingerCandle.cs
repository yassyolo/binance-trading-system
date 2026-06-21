namespace BollingerIndicatorService.Models;

public sealed record BollingerCandle
{
    public long Time { get; init; }
    public long CloseTime { get; init; }
    public decimal Open { get; init; }
    public decimal Close { get; init; }
}