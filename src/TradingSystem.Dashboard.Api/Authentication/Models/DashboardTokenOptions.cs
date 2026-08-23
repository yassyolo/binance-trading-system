namespace TradingSystem.Dashboard.Api.Authentication.Models;

public sealed record DashboardTokenOptions(
    string Issuer,
    string Audience,
    string SigningKey,
    int LifetimeHours);
