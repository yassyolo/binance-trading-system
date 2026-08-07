namespace TradingSystem.Strategies.Alligator.Models;

public sealed record AlligatorEntryParameters(
    string Symbol,
    string Interval,
    bool EnableLong,
    bool EnableShort,
    bool UseMa200Filter,
    decimal MinimumCandleRange);
