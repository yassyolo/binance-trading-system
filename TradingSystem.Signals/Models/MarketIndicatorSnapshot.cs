namespace TradingSystem.Signals.Models;

public sealed record MarketIndicatorSnapshot(
    string Symbol,
    string Interval,
    DateTime CandleOpenTimeUtc,
    DateTime CandleCloseTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume,
    IReadOnlyDictionary<string, decimal> Indicators)
{
    public bool TryGet(string key, out decimal value)
        => Indicators.TryGetValue(key, out value);
}
