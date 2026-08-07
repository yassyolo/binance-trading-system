namespace TradingSystem.Strategies.Grid.Models;

public sealed record PolicyDecision(bool Allowed, string Reason)
{
    public static PolicyDecision Allow(string reason)
        => new(true, reason);

    public static PolicyDecision Block(string reason)
        => new(false, reason);
}
