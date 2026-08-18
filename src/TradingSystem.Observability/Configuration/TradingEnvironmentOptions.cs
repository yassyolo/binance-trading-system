namespace TradingSystem.Observability.Configuration;

public sealed class TradingEnvironmentOptions
{
    public const string SectionName = "TradingEnvironment";

    public string EnvironmentName { get; set; } = "Paper";
}
