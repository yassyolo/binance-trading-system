namespace TradingSystem.Jobs.Worker.Configuration;

public sealed record JobWorkerOptions
{
    public const string SectionName = "JobWorkers";
    public bool Enabled{ get; init; } = true;
    public int PollSeconds{ get; init; } = 2;
    public int BatchSize{ get; init; } = 2;
    public int ProcessingTimeoutMinutes{ get; init; } = 30;
    public int MaximumAttempts{ get; init; } = 3;
    public string Interval{ get; init; } = "1m";
}