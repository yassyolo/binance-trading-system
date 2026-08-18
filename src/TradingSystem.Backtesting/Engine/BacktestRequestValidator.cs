using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Engine;

internal static class BacktestRequestValidator
{
    public static void Validate(BacktestRequest request,  SymbolTradingRules rules)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(rules);
        
        if (string.IsNullOrWhiteSpace(request.StrategyName))
            throw new ArgumentException("StrategyName is required.",  nameof(request));
        
        if (string.IsNullOrWhiteSpace(request.Symbol))
            throw new ArgumentException("Symbol is required.",  nameof(request));
        
        if (string.IsNullOrWhiteSpace(request.Interval))
            throw new ArgumentException("Interval is required.",  nameof(request));
        
        if (request.InitialBalance <= 0)
            throw new ArgumentOutOfRangeException(nameof(request),  "InitialBalance must be positive.");
        
        if (request.RiskPerTradePercent is <= 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(request),  "RiskPerTradePercent must be in (0,  100].");
        
        if (request.SlippageBasisPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(request),  "SlippageBasisPoints cannot be negative.");
        
        if (request.StartUtc.HasValue  &&  request.EndUtc.HasValue  &&  request.EndUtc <= request.StartUtc)
            throw new ArgumentException("EndUtc must be after StartUtc.",  nameof(request));
        
        if (rules.TickSize <= 0 || rules.QuantityStep <= 0 || rules.MinimumQuantity <= 0 || rules.MaximumQuantity < rules.MinimumQuantity)
            throw new ArgumentException("Invalid symbol trading rules.",  nameof(rules));
       
        if (rules.ContractMultiplier <= 0 || rules.Leverage <= 0)
            throw new ArgumentException("ContractMultiplier and Leverage must be positive.",  nameof(rules));
    }
}
