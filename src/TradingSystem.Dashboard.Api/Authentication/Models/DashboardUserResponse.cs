namespace TradingSystem.Dashboard.Api.Authentication.Models;

public sealed record DashboardUserResponse(
    string UserName,
    string DisplayName,
    IReadOnlyCollection<string> Roles);
