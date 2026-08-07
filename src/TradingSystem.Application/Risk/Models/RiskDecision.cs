namespace TradingSystem.Application.Risk.Models;

public sealed record RiskDecision(bool Allowed, string Code, string Reason)
{
    public static RiskDecision Allow(string reason = "Risk checks passed.") => new(true, "ALLOWED", reason);
    
    public static RiskDecision Block(string code, string reason) => new(false, code, reason);
}
