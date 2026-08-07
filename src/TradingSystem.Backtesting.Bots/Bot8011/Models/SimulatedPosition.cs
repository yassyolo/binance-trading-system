using TradingSystem.Backtesting.Bots.Bot8011.Models.Enums;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Bots.Bot8011.Models;

public sealed class SimulatedPosition
{
    public required string Id { get; init; }
    
    public required TradeSide Side { get; init; }
    
    public required DateTime EntryTimeUtc { get; init; }
    
    public required decimal EntryPrice { get; init; }
    
    public required decimal InitialQuantity { get; init; }
    
    public required decimal RemainingQuantity { get; set; }
    
    public required decimal InitialStopLoss { get; init; }
    
    public required decimal TakeProfit { get; init; }
   
    public decimal? Stop3Current { get; set; }
   
    public LifecycleStage Stage { get; set; }
    
    public decimal RealizedGross { get; set; }
    
    public decimal Fees { get; set; }
    
    public bool PartialTpReached { get; set; }
}
