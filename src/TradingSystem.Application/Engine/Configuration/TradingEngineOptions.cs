namespace TradingSystem.Application.Engine.Configuration;

public sealed class TradingEngineOptions
{
    public const string SectionName = "TradingEngine";
      
    public TimeSpan ProcessingIdempotencyTtl { get; set; } = TimeSpan.FromMinutes(5);
    
    public TimeSpan CompletedIdempotencyTtl { get; set; } = TimeSpan.FromHours(24);
    
    public TimeSpan OperationLockTtl { get; set; } = TimeSpan.FromSeconds(30);
    
    public TimeSpan MaximumSignalAge { get; set; } = TimeSpan.FromMinutes(2);
    
    public TimeSpan MaximumFutureClockSkew { get; set; } = TimeSpan.FromSeconds(10);
   
    public int PostExecutionCompletionRetryCount { get; set; } = 3;
    
    public TimeSpan PostExecutionCompletionRetryDelay { get; set; } = TimeSpan.FromMilliseconds(250);
}
