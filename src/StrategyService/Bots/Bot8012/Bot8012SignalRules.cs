namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012SignalRules
{
    public bool Enabled { get; set; } = true;

    public bool RequireBollingerBreakout { get; set; } = true;

    public bool RequireSmmaAlignment { get; set; } = false;

    public bool RequireAlligatorAlignment { get; set; } = true;
}
