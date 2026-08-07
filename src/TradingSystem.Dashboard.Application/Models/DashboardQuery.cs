namespace TradingSystem.Dashboard.Application.Models;

public sealed record DashboardQuery(
    int Skip = 0, 
    int Take = 100, 
    string? BotName = null, 
    string? Symbol = null, 
    DateTime? FromUtc = null, 
    DateTime? ToUtc = null,
    string? Status = null);




