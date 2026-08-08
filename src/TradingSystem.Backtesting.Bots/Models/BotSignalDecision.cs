using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Bots.Models;

public sealed record BotSignalDecision(
    DateTime TimeUtc,
    TradeSide Side,
    string Decision,
    string Reason,
    decimal ReferencePrice,
    string? SignalId);
