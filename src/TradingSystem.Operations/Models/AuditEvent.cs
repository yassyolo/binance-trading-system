namespace TradingSystem.Operations.Models;

public sealed record AuditEvent(
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
    string? NewValueJson, 
    IReadOnlyDictionary<string, string>? Metadata = null);