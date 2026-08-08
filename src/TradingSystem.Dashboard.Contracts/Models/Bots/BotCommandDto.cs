using TradingSystem.Dashboard.Contracts.Models.Enums;

namespace TradingSystem.Dashboard.Contracts.Models.Bots;

public sealed record BotCommandDto(
    Guid CommandId,
    string BotName, 
    BotCommandType Command, 
    BotCommandStatus Status, 
    string RequestedBy, 
    string Reason, 
    DateTime RequestedAtUtc, 
    DateTime? CompletedAtUtc, 
    string? Error);
