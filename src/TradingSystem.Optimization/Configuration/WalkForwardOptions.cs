namespace TradingSystem.Optimization.Configuration;

public sealed record WalkForwardOptions
{
    public int TrainingBars { get; init; } = 10_000;
    
    public int TestingBars { get; init; } = 2_000;
    
    public int StepBars { get; init; } = 2_000;
       
    public bool AnchoredTraining { get; init; }
    
    public int TopCandidatesPerWindow { get; init; } = 1;
}
