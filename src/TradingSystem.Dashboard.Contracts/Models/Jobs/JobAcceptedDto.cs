namespace TradingSystem.Dashboard.Contracts.Models.Jobs;

public sealed record JobAcceptedDto(Guid JobId, string Type, string Status, DateTime CreatedAtUtc);
