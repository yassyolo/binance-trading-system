using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Bots.Models;

public sealed record HistoricalBotSignal(
    DateTime TimeUtc, 
    TradeSide Side, 
    string Source  =  "historical", 
    string? SignalId  =  null);
