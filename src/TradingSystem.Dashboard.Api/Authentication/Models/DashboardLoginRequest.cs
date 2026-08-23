namespace TradingSystem.Dashboard.Api.Authentication.Models;

public sealed record DashboardLoginRequest(
    string UserName,
    string Password);
