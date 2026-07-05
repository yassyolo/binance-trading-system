using TradingSystem.Domain.Healing;

namespace StrategyService.Services.Healing;

public interface IBotHealingService 
{ 
    string BotName { get; } 
    Task HealAsync(HealingSnapshot snapshot,
        CancellationToken cancellationToken);
}
