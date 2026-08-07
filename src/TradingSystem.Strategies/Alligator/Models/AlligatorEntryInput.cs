namespace TradingSystem.Strategies.Alligator.Models;

public sealed record AlligatorEntryInput(
    string Symbol,
    string Interval,
    bool IsClosed,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Teeth,
    decimal Sma200);
