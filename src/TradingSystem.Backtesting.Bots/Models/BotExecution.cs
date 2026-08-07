using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Bots.Models;

public sealed record BotExecution(
    string PositionId,
    DateTime TimeUtc,
    string Type,
    TradeSide Side,
    decimal Price,
    decimal Quantity,
    decimal GrossPnl,
    decimal Fee,
    string Reason);
