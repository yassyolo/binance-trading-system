namespace TradingSystem.Analytics.Models;

public sealed record WalkForwardWindow
{
    public required Guid WindowId { get; init; }
   
    public required Guid RunId { get; init; }
   
    public required int WindowNumber { get; init; }
    
    public required DateTime TrainFromUtc { get; init; }
    
    public required DateTime TrainToUtc { get; init; }
   
    public required DateTime TestFromUtc { get; init; }
   
    public required DateTime TestToUtc { get; init; }
    
    public required string SelectedParametersJson { get; init; }
    
    public required decimal InSampleScore { get; init; }
   
    public required decimal OutOfSampleScore { get; init; }
   
    public required PerformanceMetricSet InSampleMetrics { get; init; }
    
    public required PerformanceMetricSet OutOfSampleMetrics { get; init; }
}