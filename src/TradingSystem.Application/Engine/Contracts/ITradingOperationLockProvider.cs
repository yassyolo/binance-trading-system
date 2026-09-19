using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Engine.Contracts;

public interface ITradingOperationLockProvider
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string botName, 
        string symbol, 
        PositionSide side, 
        TimeSpan ttl, 
        CancellationToken ct);
}
