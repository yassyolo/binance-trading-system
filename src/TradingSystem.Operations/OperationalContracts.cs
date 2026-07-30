using TradingSystem.Operations.Enums;

namespace TradingSystem.Operations;

public sealed record ServiceHeartbeat(
    string ServiceName, 
    string InstanceId, 
    string Version, 
    string Environment, 
    OperationalStatus Status, 
    DateTime StartedAtUtc, 
    DateTime LastSeenAtUtc, 
    int StaleAfterSeconds, 
    IReadOnlyDictionary<string, string>? Details  =  null);

public sealed record AlertCandidate(
    string DeduplicationKey, 
    AlertSeverity Severity, 
    string Type, 
    string Message, 
    string? BotName  =  null, 
    string? PositionId  =  null, 
    IReadOnlyDictionary<string, string>? Metadata  =  null);

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
    IReadOnlyDictionary<string, string>? Metadata  =  null);

public interface IServiceHeartbeatStore { Task UpsertAsync(ServiceHeartbeat heartbeat, CancellationToken ct); }

public interface IAlertStore
{
    Task UpsertActiveAsync(AlertCandidate alert, CancellationToken ct);
    Task ResolveMissingAsync(string sourcePrefix, IReadOnlyCollection<string> activeKeys, CancellationToken ct);
}

public interface IAlertCandidateSource 
{ 
    string SourcePrefix { get; } 
    
    Task<IReadOnlyCollection<AlertCandidate>> LoadAsync(CancellationToken ct); 
}

public interface IAuditLog 
{ 
    Task WriteAsync(AuditEvent auditEvent, CancellationToken ct); 
}

public sealed class ServiceHeartbeatOptions
{
    public const string SectionName = "ServiceHeartbeat";
    
    public bool Enabled{ get; set;} = true;
    
    public string ServiceName{ get; set;} = "UnknownService";
    
    public string Environment{ get; set;} = "Demo";
    
    public int IntervalSeconds{ get; set;} = 10;
    
    public int StaleAfterSeconds{ get; set;} = 30;
}

public sealed class AlertEngineOptions
{
    public const string SectionName = "AlertEngine";
    
    public bool Enabled{ get; set;} = true;
    
    public int PollSeconds{ get; set;} = 15;
}
