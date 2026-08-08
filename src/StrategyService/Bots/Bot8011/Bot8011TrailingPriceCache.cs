using TradingSystem.Application.Time;

namespace StrategyService.Bots.Bot8011;

public sealed class Bot8011TrailingPriceCache(IClock clock)
{
    readonly object gate = new();
    decimal? price;DateTime at;
    
    public void Set(decimal x)
    {
        lock(gate)
        {
            price = x;
            at = clock.UtcNow;
        }
    }
    
    public bool TryGetFresh(TimeSpan age, out decimal x)
    {
        lock(gate)
        {
            x = price ?? 0;
            
            return price > 0 && clock.UtcNow - at <= age;
        }
    }
}
