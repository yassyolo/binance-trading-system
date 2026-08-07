namespace TradingSystem.Strategies.Positions.Models;

public sealed record PositionAdmissionParameters(
    bool EnableLong,
    bool EnableShort,
    int SideLimit,
    bool CloseOppositeFirst);
