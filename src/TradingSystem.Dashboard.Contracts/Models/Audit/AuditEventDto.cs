namespace TradingSystem.Dashboard.Contracts.Models.Audit;

public sealed record AuditEventDto(
    Guid AuditId,
    DateTime OccurredAtUtc, 
    string Actor, 
    string Action, 
    string EntityType,
    string? EntityId,
    string? Reason,
    string? CorrelationId,
    string? IpAddress, 
    string? OldValueJson, 
    string? NewValueJson);
