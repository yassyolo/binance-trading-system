using TradingSystem.Backtesting.Bots.Models.Enums;

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
