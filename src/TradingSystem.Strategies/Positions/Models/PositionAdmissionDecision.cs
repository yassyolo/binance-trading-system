namespace TradingSystem.Strategies.Positions.Models;

public sealed record PositionAdmissionDecision(
    bool Allowed,
    IReadOnlyCollection<string> PositionsToClose,
    string Reason);
