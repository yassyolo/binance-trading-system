using TradingSystem.Dashboard.Contracts.Models.Enums;

namespace TradingSystem.Dashboard.Contracts.Models.Bots;

public sealed record BotCommandRequest(
    BotCommandType Command, 
    string Reason,
    bool Confirmed, 
    bool CancelOpenOrders = false,
    bool CloseOpenPositions = false,
    string? PositionId = null);
