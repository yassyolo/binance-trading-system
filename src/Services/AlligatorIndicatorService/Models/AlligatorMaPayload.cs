namespace AlligatorIndicatorService.Models;

public sealed record AlligatorMaPayload
{
    public string Type { get; init; } = "alligator_ma";
    public string Symbol { get; init; } = string.Empty;
    public string Timeframe { get; init; } = string.Empty;
    public long CandleCloseTime { get; init; }
    public long PublishedAt { get; init; }

    public AlligatorIndicators Indicators { get; init; } = new();
}

public sealed record AlligatorIndicators
{
    public IndicatorValue AlligatorJaw { get; init; } = new();
    public IndicatorValue AlligatorTeeth { get; init; } = new();
    public IndicatorValue AlligatorLips { get; init; } = new();
    public IndicatorValue Sma200 { get; init; } = new();
}

public sealed record IndicatorValue
{
    public decimal Value { get; init; }
}