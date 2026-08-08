namespace TradingSystem.Dashboard.Contracts.Models.Health;

public sealed record ComponentHealthDto(string Component, string Status, DateTime? LastSeenUtc, string? Details);

