using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Bots.Common;

public sealed record HistoricalBotSignal(
    DateTime TimeUtc, 
    TradeSide Side, 
    string Source  =  "historical", 
    string? SignalId  =  null);
