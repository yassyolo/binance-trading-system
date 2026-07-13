namespace AlligatorIndicatorService.Models;

public sealed record Candle
{
    public string Symbol { get; init; } = string.Empty;
    public string Interval { get; init; } = string.Empty;

    public long OpenTime { get; init; }
    public long CloseTime { get; init; }

    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }

    public bool IsClosed { get; init; }
}