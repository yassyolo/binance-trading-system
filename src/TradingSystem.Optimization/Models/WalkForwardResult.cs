namespace TradingSystem.Optimization.Models;

public sealed record WalkForwardResult<TOptions>
{
    public required string BotName { get; init; }
    
    public required DateTime StartedAtUtc { get; init; }
   
    public required DateTime CompletedAtUtc { get; init; }
   
    public required IReadOnlyList<WalkForwardWindowResult<TOptions>> Windows { get; init; }        
}
