using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/runs")]
public sealed class RunsController(
    IDashboardQueryStore store)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(string? botName, int skip, int take, CancellationToken ct)
    {
        var result = await store.GetRunsAsync(new(RequestValidation.Skip(skip), RequestValidation.PageSize(take), botName), ct);

        return Ok(result);
    }
}
