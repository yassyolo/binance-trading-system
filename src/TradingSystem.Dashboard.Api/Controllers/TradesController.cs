using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/trades")]
public sealed class TradesController(
    IDashboardQueryStore queryStore)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(string? botName, string? symbol, int skip, int take, CancellationToken ct)
    {
        var result = await queryStore.GetTradesAsync(new(RequestValidation.Skip(skip), RequestValidation.PageSize(take), botName, symbol), ct);

        return Ok(result);
    }
}
