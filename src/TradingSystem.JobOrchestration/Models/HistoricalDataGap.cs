namespace TradingSystem.JobOrchestration.Models;

public sealed record HistoricalDataGap(
    string Symbol, 
    string Interval, 
    DateTime FromUtc,
    DateTime ToUtc, 
    int MissingCandles);

