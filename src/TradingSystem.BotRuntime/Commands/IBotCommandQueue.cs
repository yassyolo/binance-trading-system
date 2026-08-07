using TradingSystem.BotRuntime.Commands.Models;

namespace TradingSystem.BotRuntime.Commands;

public interface IBotCommandQueue
{
    Task<IReadOnlyCollection<BotCommand>> ClaimPendingAsync(string workerId, int batchSize, TimeSpan processingTimeout, CancellationToken ct);
    
    Task CompleteAsync(Guid commandId, string workerId, CancellationToken ct);
   
    Task FailAsync(Guid commandId, string workerId, string error, bool retryable, CancellationToken ct);
   
    Task RejectAsync(Guid commandId, string workerId, string reason, CancellationToken ct);
}
