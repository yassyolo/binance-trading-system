using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Bots.Models;

public sealed record HistoricalBotSignal(
    DateTime TimeUtc, 
    TradeSide Side, 
    string Source = "historical", 
    string? SignalId = null);
