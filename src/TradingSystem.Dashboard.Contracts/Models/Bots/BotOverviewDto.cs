using TradingSystem.Dashboard.Contracts.Models.Enums;

namespace TradingSystem.Dashboard.Contracts.Models.Bots;

public sealed record BotOverviewDto(
    string BotName, BotRuntimeStatus Status, TradingEnvironment Environment,
    string SignalSource, string? LastSignalSide, DateTime? LastSignalAtUtc,
    string? LastDecision, string? LastDecisionReason, DateTime? LastDecisionAtUtc,
    int OpenPositions, decimal UnrealizedPnl, decimal RealizedPnlToday,
    string StrategyVersion, DateTime? LastHeartbeatUtc);

