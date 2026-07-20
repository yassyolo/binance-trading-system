using TradingSystem.Application.Positions;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Risk;

public sealed record RiskEvaluationContext
{
    public required TradeSignal Signal {  get;  init;  }
    public required decimal MarkPrice {  get;  init;  }
    public required IReadOnlyCollection<ActivePositionView> ActivePositions {  get;  init;  }
    public required DateTime EvaluatedAtUtc {  get;  init;  }
}

public sealed record RiskDecision(bool Allowed,  string Code,  string Reason)
{
    public static RiskDecision Allow(string reason  =  "Risk checks passed.")  =>  new(true,  "ALLOWED",  reason);
    public static RiskDecision Block(string code,  string reason)  =>  new(false,  code,  reason);
}

public interface ICentralRiskManager
{
    Task<RiskDecision> EvaluateOpenAsync(RiskEvaluationContext context,  CancellationToken cancellationToken);
}

public sealed class NullCentralRiskManager : ICentralRiskManager
{
    public Task<RiskDecision> EvaluateOpenAsync(RiskEvaluationContext context,  CancellationToken cancellationToken)
         =>  Task.FromResult(RiskDecision.Allow("Central risk management is not configured."));
}
