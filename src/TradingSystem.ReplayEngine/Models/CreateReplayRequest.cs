using TradingSystem.ReplayEngine.Models.Enums;

namespace TradingSystem.ReplayEngine.Models;

public sealed record CreateReplayRequest(
    string Name,
    ReplayMode Mode,
    long? FromGlobalPosition = null,
    long? ToGlobalPosition = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? BotName = null,
    string? Symbol = null,
    string? CorrelationId = null,
    string? CandidateStrategyPluginId = null,
    string? CandidateStrategyVersion = null,
    int BatchSize = 250,
    bool StopOnError = true);
