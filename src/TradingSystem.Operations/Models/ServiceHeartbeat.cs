using TradingSystem.Operations.Models.Enums;

namespace TradingSystem.Operations.Models;

public sealed record ServiceHeartbeat(
    string ServiceName,
    string InstanceId,
    string Version,
    string Environment,
    OperationalStatus Status,
    DateTime StartedAtUtc,
    DateTime LastSeenAtUtc,
    int StaleAfterSeconds,
    IReadOnlyDictionary<string, string>? Details = null);
