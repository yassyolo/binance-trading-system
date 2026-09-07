using Microsoft.AspNetCore.Mvc;

namespace TradingSystem.Dashboard.Api.Controllers;

public abstract class DashboardControllerBase : ControllerBase
{
    protected string DashboardUserName => User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "dashboard-user";
}
