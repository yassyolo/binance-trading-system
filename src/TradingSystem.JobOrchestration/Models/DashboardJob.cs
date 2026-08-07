using TradingSystem.JobOrchestration.Models.Enums;

namespace TradingSystem.JobOrchestration.Models;

public sealed record DashboardJob(
    Guid JobId, 
    string Type,
    DashboardJobStatus Status, 
    string RequestedBy, 
    string RequestJson, 
    int AttemptCount, 
    DateTime CreatedAtUtc, 
    DateTime? StartedAtUtc);

