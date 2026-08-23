namespace TradingSystem.Dashboard.Api.Authentication.Models;

public sealed record DashboardCredentials(
    string UserName,
    string PasswordHash);
