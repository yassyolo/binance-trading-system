using TradingSystem.Application.Risk.Models;

namespace TradingSystem.Application.Risk.Contracts;

public interface ICentralRiskManager
{
    Task<RiskDecision> EvaluateOpenAsync(RiskEvaluationContext context, CancellationToken ct);
}

