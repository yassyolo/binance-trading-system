namespace TradingSystem.Dashboard.Api.Authentication.Models;

public sealed record DashboardLoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    string UserName,
    string DisplayName,
    IReadOnlyCollection<string> Roles);
