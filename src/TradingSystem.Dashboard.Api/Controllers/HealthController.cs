using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController(
    IDashboardQueryStore store)
    : DashboardControllerBase
{
    [HttpGet("components")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetComponentsAsync(
        CancellationToken cancellationToken)
    {
        var result = await store.GetHealthAsync(
            cancellationToken);

        return Ok(result);
    }
}
