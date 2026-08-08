using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/positions")]
public sealed class PositionsController(
    IDashboardQueryStore store)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(
        string? botName,
        string? symbol,
        string? status,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var result = await store.GetPositionsAsync(
            new(
                RequestValidation.Skip(skip),
                RequestValidation.PageSize(take),
                botName,
                symbol,
                Status: status),
            cancellationToken);

        return Ok(result);
    }
}
