namespace TradingSystem.Dashboard.Contracts.Models.Optimization;

public sealed record OptimizationRequest(
    string BotName, 
    string Symbol, 
    DateTime FromUtc, 
    DateTime ToUtc, 
    decimal InitialBalance, 
    string SignalSource, 
    IReadOnlyCollection<OptimizationRangeDto> Ranges, 
    int TopResults, 
    bool WalkForward, 
    int? TrainBars, 
    int? TestBars, 
    int? StepBars);