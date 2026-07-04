namespace BollingerIndicatorService.Models;

public sealed record BollingerPayload
{
    public string Type { get; init; } = "bb";
    public string Symbol { get; init; } = string.Empty;
    public string Timeframe { get; init; } = string.Empty;
    public long CandleOpenTime { get; init; }
    public long CandleCloseTime { get; init; }
    public long PublishedAt { get; init; }

    public Dictionary<string, BollingerIndicatorValue> Indicators { get; init; } = new();
}

public sealed record BollingerIndicatorValue
{
    public int Length { get; init; }
    public string Source { get; init; } = string.Empty;
    public string MaType { get; init; } = "SMA";
    public decimal Mult { get; init; }

    public decimal BasisCurrent { get; init; }
    public decimal? BasisPrev { get; init; }

    public decimal UpperCurrent { get; init; }
    public decimal? UpperPrev { get; init; }

    public decimal LowerCurrent { get; init; }
    public decimal? LowerPrev { get; init; }
}