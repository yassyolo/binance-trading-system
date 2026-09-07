namespace TradingSystem.Dashboard.Contracts.Models.Alerts;

public sealed record AlertDto(
    long AlertId,
    string Severity, 
    string Type, 
    string Message,
    string? BotName, 
    string? PositionId,
    DateTime CreatedAtUtc,
    bool Acknowledged, 
    DateTime? AcknowledgedAtUtc, 
    string? AcknowledgedBy);