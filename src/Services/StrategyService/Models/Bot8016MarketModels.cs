using TradingSystem.Domain.Enums;

namespace StrategyService.Models;

public sealed record Bot8016Candle(
    string Symbol,
    string Interval,
    long OpenTime,
    long CloseTime,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    bool IsClosed);

public sealed record Bot8016IndicatorSnapshot(
    string Symbol,
    string Timeframe,
    long CandleCloseTime,
    long PublishedAt,
    decimal Jaw,
    decimal Teeth,
    decimal Lips,
    decimal Sma200)
{
    public DateTime PublishedAtUtc
        => DateTimeOffset.FromUnixTimeMilliseconds(PublishedAt).UtcDateTime;
}

public sealed record Bot8016EntrySignal(
    PositionSide Side,
    Bot8016Candle Candle,
    Bot8016IndicatorSnapshot Indicators,
    string Reason);
