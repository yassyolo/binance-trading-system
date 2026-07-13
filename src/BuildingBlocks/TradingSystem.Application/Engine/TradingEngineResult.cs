namespace TradingSystem.Application.Engine;

public sealed record TradingEngineResult(
    bool Succeeded,
    bool OpenedPosition,
    bool Duplicate,
    string Reason,
    string? ShortId = null);
