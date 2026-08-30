namespace TradingSystem.Persistence.PostgreSql.Reliability.Models;

public sealed record UnresolvedFindingRow(
    Guid Id,
    string BotName,
    string Symbol,
    string? ShortId,
    string FindingType);
