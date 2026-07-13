using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Engine;

public interface ISignalCooldownStore
{
    Task<TimeSpan?> GetRemainingAsync(
        string botName,
        string symbol,
        PositionSide side,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task SetAsync(
        string botName,
        string symbol,
        PositionSide side,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken);
}