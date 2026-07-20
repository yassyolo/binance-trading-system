using TradingSystem.Contracts.UserStream;
namespace TradingSystem.Application.Healing;
public interface IBotHealingService
{
    string BotName {  get;  }
    Task HealAsync(HealingSnapshotMessage snapshot,  CancellationToken cancellationToken);
}
