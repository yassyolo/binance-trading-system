using TradingSystem.Operations.Models;

namespace TradingSystem.Operations.Contracts;

public interface IAuditLog
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken ct);
}
