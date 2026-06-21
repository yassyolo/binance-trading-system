namespace AlligatorIndicatorService.Models;

public sealed record Candle
{
    public long Time { get; init; }
    public long CloseTime { get; init; }

    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
}