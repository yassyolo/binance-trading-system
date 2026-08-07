using TradingSystem.Application.Risk.Contracts;
using TradingSystem.Application.Risk.Models;

namespace TradingSystem.Application.Risk;

public sealed class NullCentralRiskManager : ICentralRiskManager
{
    public Task<RiskDecision> EvaluateOpenAsync(RiskEvaluationContext context, CancellationToken ct)
         => Task.FromResult(RiskDecision.Allow("Central risk management is not configured."));
}
