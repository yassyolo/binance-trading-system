using TradingSystem.Backtesting.Bots.Models.Enums;

namespace TradingSystem.Backtesting.Bots.Models;

public sealed record BotPositionResult
{
    public required string PositionId { get; init; }
    
    public required TradeSide Side { get; init; }
   
    public required DateTime EntryTimeUtc { get; init; }
   
    public required decimal EntryPrice { get; init; }
   
    public required DateTime ExitTimeUtc { get; init; }
   
    public required decimal ExitPrice { get; init; }
    
    public required decimal Quantity { get; init; }
    
    public required decimal GrossPnl { get; init; }
   
    public required decimal Fees { get; init; }
   
    public required decimal NetPnl { get; init; }
    
    public required string ExitReason { get; init; }
   
    public bool PartialTakeProfitReached { get; init; }
}