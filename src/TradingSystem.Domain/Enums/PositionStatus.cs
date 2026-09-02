namespace TradingSystem.Domain.Enums;

public enum PositionStatus
{
    New = 1, 
    ParentFilled = 2, 
    Open = 3, 
    TpFilled = 4, 
    Stop3Pending = 5, 
    Stop3Active = 6, 
    Closing = 7, 
    Closed = 8, 
    Failed = 9, 
    Test = 10
}
