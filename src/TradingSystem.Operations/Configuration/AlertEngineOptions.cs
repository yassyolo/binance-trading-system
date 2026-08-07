namespace TradingSystem.Operations.Configuration;

public sealed class AlertEngineOptions
{
    public const string SectionName = "AlertEngine";

    public bool Enabled { get; set; } = true;

    public int PollSeconds { get; set; } = 15;
}
