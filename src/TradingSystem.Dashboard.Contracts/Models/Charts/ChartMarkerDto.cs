namespace TradingSystem.Dashboard.Contracts.Models.Charts;

public sealed record ChartMarkerDto(
    DateTime TimeUtc,
    string Kind, 
    string Side, 
    decimal Price, 
    string? Label);

