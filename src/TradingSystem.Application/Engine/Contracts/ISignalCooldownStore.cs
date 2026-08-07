using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Engine.Contracts;

public interface ISignalCooldownStore
{
    Task<TimeSpan?> GetRemainingAsync(string botName, string symbol, PositionSide side, DateTime nowUtc, CancellationToken ct);
    
    Task SetAsync(string botName, string symbol, PositionSide side, DateTime expiresAtUtc, CancellationToken ct);
}
