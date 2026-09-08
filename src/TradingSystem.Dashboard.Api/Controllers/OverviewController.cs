using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/overview")]
public sealed class OverviewController(
    IDashboardQueryStore queryStore)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        var result = await queryStore.GetOverviewAsync(ct);

        return Ok(result);
    }
}
