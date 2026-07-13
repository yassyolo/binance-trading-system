using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Strategies;

public sealed record StrategyMetadata(
    string Name,
    string Version,
    PositionMode PositionMode,
    IReadOnlyCollection<string> SupportedSymbols);
