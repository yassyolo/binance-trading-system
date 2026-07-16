namespace StrategyService.Bot8012.Signals.Configuration;

public sealed class Bot8012SignalRuleOptions
{
    public bool Enabled { get; set; } = true;
    public bool RequireBollingerBreakout { get; set; } = true;
    public bool RequireSmmaAlignment { get; set; } = true;
    public bool RequireAlligatorAlignment { get; set; } = true;
}

public sealed class Bot8012SignalGeneratorOptions
{
    public const string SectionName = "Bots:Bot8012";
    public string BotName { get; set; } = "BOT8012";
    public string StrategyVersion { get; set; } = "1.0.0";
    public string Symbol { get; set; } = "BTCUSDC";
    public bool EnableLong { get; set; } = true;
    public bool EnableShort { get; set; } = true;
    public Bot8012SignalRuleOptions SignalRules { get; set; } = new();
}
