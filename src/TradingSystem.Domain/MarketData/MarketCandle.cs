namespace TradingSystem.Domain.MarketData;

public sealed record MarketCandle(
    string Symbol, 
    string Interval, 
    DateTime OpenTimeUtc, 
    DateTime CloseTimeUtc, 
    decimal Open, 
    decimal High, 
    decimal Low, 
    decimal Close, 
    decimal Volume, 
    bool IsClosed = true);
