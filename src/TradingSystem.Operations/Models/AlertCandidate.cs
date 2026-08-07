using TradingSystem.Operations.Models.Enums;

namespace TradingSystem.Operations.Models;

public sealed record AlertCandidate(
    string DeduplicationKey,
    AlertSeverity Severity,
    string Type,
    string Message,
    string? BotName = null,
    string? PositionId = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
