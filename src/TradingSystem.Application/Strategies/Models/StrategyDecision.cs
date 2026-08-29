using TradingSystem.Application.Strategies.Models.Enums;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Strategies.Models;

public sealed record StrategyDecision
{
    private StrategyDecision(StrategyDecisionType type, PositionSide side, string reason, IReadOnlyCollection<string>? positionsToClose = null)
    {
        Type = type;
        Side = side;
        Reason = reason;
        PositionsToClose = positionsToClose ?? [];
    }

    public StrategyDecisionType Type { get; }
    
    public PositionSide Side { get; }
   
    public string Reason { get; }
    
    public IReadOnlyCollection<string> PositionsToClose { get; }
   
    public bool ShouldOpen => Type is StrategyDecisionType.Open or StrategyDecisionType.OpenAfterClosing;
   
    public bool ShouldClosePositions => Type == StrategyDecisionType.OpenAfterClosing;

    
    public static StrategyDecision Open(PositionSide side, string reason)  
        => new(StrategyDecisionType.Open, side, reason);
    
    public static StrategyDecision OpenAfterClosing(PositionSide side, IReadOnlyCollection<string> positionsToClose, string reason)
    {
        ArgumentNullException.ThrowIfNull(positionsToClose);
        
        if (positionsToClose.Count == 0)
            throw new ArgumentException("At least one position must be provided.",  nameof(positionsToClose));
       
        return new(StrategyDecisionType.OpenAfterClosing, side, reason, positionsToClose);
    }
    
    public static StrategyDecision Block(PositionSide side, string reason)  
        => new(StrategyDecisionType.Ignore, side, reason);
}
