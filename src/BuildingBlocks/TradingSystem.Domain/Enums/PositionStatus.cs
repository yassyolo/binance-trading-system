namespace TradingSystem.Domain.Enums;

public enum PositionStatus
{
    New = 1,
    ParentFilled = 2,
    Active = 3,
    TpExecuted = 4,
    Stop3Active = 5,
    Closing = 6,
    Closed = 7,
    Failed = 8,
    Test = 9,
    Open = 10,
}