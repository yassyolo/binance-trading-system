namespace StrategyService.Bots.Bot8016.Models;

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

