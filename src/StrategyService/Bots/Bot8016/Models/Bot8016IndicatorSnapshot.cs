namespace StrategyService.Bots.Bot8016.Models;

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
