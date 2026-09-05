namespace TradingSystem.Backtesting.Bots.Bot8016.Models;

public sealed record HistoricalAlligatorSnapshot(
    DateTime TimeUtc, 
    string Symbol, 
    string Interval, 
    decimal Jaw, 
    decimal Teeth, 
    decimal Lips, 
    decimal Sma200);