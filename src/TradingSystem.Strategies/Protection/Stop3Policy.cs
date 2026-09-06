using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Protection.Models;

namespace TradingSystem.Strategies.Protection;

public sealed class Stop3Policy
{
    public decimal InitialTrigger(PositionSide side, decimal entry, Stop3Parameters p) 
        => side == PositionSide.Long ? entry + p.EntryOffset : entry - p.EntryOffset;
    
    public decimal NextTrigger(PositionSide side, decimal marketPrice, decimal current, Stop3Parameters p)
    {
        if(side == PositionSide.Long)
            return marketPrice >= current + p.TrailingStep ? current + p.TrailingBuffer : current;
      
        return marketPrice <= current - p.TrailingStep ? current - p.TrailingBuffer : current;
    }
    
    public bool Breakout(PositionSide side, decimal close, decimal? signalHigh, decimal? signalLow) 
        => side == PositionSide.Long 
        ? signalHigh is > 0 && close > signalHigh 
        : signalLow is > 0 && close < signalLow;
   
    public bool TeethExit(PositionSide side, decimal close, decimal teeth) 
        => side == PositionSide.Long 
        ? close < teeth 
        : close > teeth;
}