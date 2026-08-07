using TradingSystem.Backtesting.Bots.Common;

namespace TradingSystem.Optimization.Models;

public sealed record WalkForwardWindowResult<TOptions>
{
    public required int WindowNumber { get; init; }
   
    public required DateTime TrainFromUtc { get; init; }
   
    public required DateTime TrainToUtc { get; init; }
    
    public required DateTime TestFromUtc { get; init; }
    
    public required DateTime TestToUtc { get; init; }
    
    public required TOptions SelectedOptions { get; init; }
   
    public required decimal InSampleScore { get; init; }
   
    public required decimal OutOfSampleScore { get; init; }
    
    public required BotBacktestMetrics InSampleMetrics { get; init; }
    
    public required BotBacktestMetrics OutOfSampleMetrics { get; init; }
}
