using TradingSystem.Application.Healing.Contracts;

namespace TradingSystem.Application.Healing;

public sealed class HealingServiceRegistry
{
    private readonly IReadOnlyCollection<IBotHealingService> _services;
    
    public HealingServiceRegistry(IEnumerable<IBotHealingService> services)  
        => _services = services.ToArray();
    
    public IEnumerable<IBotHealingService> ForSymbol(string symbol) 
        => _services;
}
