using TradingSystem.Contracts.UserStream;

namespace TradingSystem.Application.Healing.Contracts;

public interface IBotHealingService
{
    string BotName { get; }
    
    Task HealAsync(HealingSnapshotMessage snapshot, CancellationToken ct);
}
